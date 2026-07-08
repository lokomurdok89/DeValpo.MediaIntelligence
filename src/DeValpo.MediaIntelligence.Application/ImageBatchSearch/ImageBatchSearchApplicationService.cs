using System.Text.Json;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.CloudAssets;
using DeValpo.MediaIntelligence.Application.ImageBatchSearch.Models;
using DeValpo.MediaIntelligence.Application.Resources;
using DeValpo.MediaIntelligence.Application.VideoSearch;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Contracts.ImageBatchSearch;
using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Application.ImageBatchSearch;

public sealed class ImageBatchSearchApplicationService(
    IMediaAssetStorage storage,
    IAnalysisProfileResolver profileResolver,
    IImageBatchSearchJobRepository jobRepository,
    IEnumerable<IImageBatchTextSearchProvider> textProviders,
    IEnumerable<IImageBatchFaceSearchProvider> faceProviders,
    IAssetReferenceResolver assetReferenceResolver,
    IUnitOfWork unitOfWork,
    IOptions<ImageBatchSearchOptions> options,
    IOptions<CloudAssetOptions> cloudAssetOptions,
    ILogger<ImageBatchSearchApplicationService> logger) : IImageBatchSearchApplicationService
{
    private const string ProfileKey = "image-batch-search-v1";
    private const string SourceImageRole = "SourceImage";
    private const string ReferenceImageRole = "ReferenceImage";

    /// <summary>
    /// Stores uploaded source and reference images and creates a pending image batch search job.
    /// </summary>
    public async Task<CreateImageBatchSearchJobResponse> EnqueueAsync(
        CreateImageBatchSearchJobCommand command,
        CancellationToken cancellationToken)
    {
        var manifest = await ValidateRequestAsync(command, cancellationToken);
        var job = new ImageBatchSearchJob
        {
            ClientKey = command.ClientKey.Trim(),
            ProfileKey = command.ProfileKey.Trim(),
            Status = AnalysisJobStatus.Pending.ToString(),
            ManifestJson = JsonSerializer.Serialize(manifest)
        };

        await jobRepository.AddAsync(job, cancellationToken);
        if (command.AssetSelector is not null &&
            !string.Equals(command.AssetSelector.SelectorMode, CloudAssetSelectorModes.MultipartUpload, StringComparison.OrdinalIgnoreCase))
        {
            await AddCloudAssetsAsync(job, command.AssetSelector, SourceImageRole, cloudAssetOptions.Value.MaxImagesPerJob, cancellationToken);
        }
        else
        {
            await StoreAssetsAsync(job, command.Images, SourceImageRole, "image", cancellationToken);
        }

        if (command.ReferenceAssetSelector is not null &&
            !string.Equals(command.ReferenceAssetSelector.SelectorMode, CloudAssetSelectorModes.MultipartUpload, StringComparison.OrdinalIgnoreCase))
        {
            await AddCloudAssetsAsync(job, command.ReferenceAssetSelector, ReferenceImageRole, options.Value.MaxTargetsPerJob, cancellationToken);
        }
        else
        {
            await StoreAssetsAsync(job, command.ReferenceImages, ReferenceImageRole, "reference", cancellationToken);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(ApplicationResourceText.ImageBatchSearchJobQueuedLogMessage);
        return new CreateImageBatchSearchJobResponse(job.Id, job.Status, job.CreatedAt);
    }

    /// <summary>
    /// Claims and processes the next pending image batch search job.
    /// </summary>
    public async Task<bool> ProcessNextPendingAsync(CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetNextPendingWithAssetsAsync(cancellationToken);
        if (job is null)
        {
            return false;
        }

        var attempt = job.ProcessingAttempts + 1;
        var claimed = await jobRepository.MarkRunningAsync(job.Id, cancellationToken);
        if (!claimed)
        {
            return true;
        }

        try
        {
            await ProcessJobAsync(job, cancellationToken);
        }
        catch (Exception exception) when (attempt < 3)
        {
            logger.LogWarning(exception, ApplicationResourceText.ImageBatchSearchJobProcessingRetryLogMessage);
            await jobRepository.SetProcessingFailureAsync(
                job.Id,
                AnalysisJobStatus.Pending.ToString(),
                exception.Message,
                completedAt: null,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, ApplicationResourceText.ImageBatchSearchJobProcessingFailedLogMessage);
            await jobRepository.SetProcessingFailureAsync(
                job.Id,
                AnalysisJobStatus.Failed.ToString(),
                exception.Message,
                DateTimeOffset.UtcNow,
                cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Returns interrupted running image batch jobs to Pending.
    /// </summary>
    public Task<int> ResetRunningJobsAsync(CancellationToken cancellationToken)
    {
        return jobRepository.ResetRunningAsync(cancellationToken);
    }

    /// <summary>
    /// Returns job metadata and current processing state.
    /// </summary>
    public async Task<ImageBatchSearchJobResponse?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        return job is null ? null : MapJob(job);
    }

    /// <summary>
    /// Maps persisted image batch results to the target-grouped API shape.
    /// </summary>
    public async Task<ImageBatchSearchResultResponse?> GetResultAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        return job is null ? null : MapResult(job, options.Value);
    }

    private async Task ProcessJobAsync(ImageBatchSearchJob job, CancellationToken cancellationToken)
    {
        var manifest = ImageBatchSearchManifestParser.Parse(job.ManifestJson, options.Value);
        var images = job.Assets
            .Where(asset => string.Equals(asset.AssetRole, SourceImageRole, StringComparison.OrdinalIgnoreCase))
            .Select(ToProviderAsset)
            .ToArray();
        var referenceImages = job.Assets
            .Where(asset => string.Equals(asset.AssetRole, ReferenceImageRole, StringComparison.OrdinalIgnoreCase))
            .Select(ToProviderAsset)
            .ToArray();

        var providerResults = new List<ImageBatchProviderResult>();
        var visibleNumberTargets = manifest.Targets
            .Where(target => string.Equals(target.QueryType, ImageBatchSearchQueryTypes.VisibleNumber, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var faceTargets = manifest.Targets
            .Where(target => string.Equals(target.QueryType, ImageBatchSearchQueryTypes.FaceReference, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var selectedTextProviders = SelectProviders(textProviders, manifest.Providers, options.Value).ToArray();
        foreach (var provider in selectedTextProviders)
        {
            if (visibleNumberTargets.Length == 0)
            {
                continue;
            }

            providerResults.Add(await provider.SearchTextAsync(
                new ImageBatchTextSearchRequest(
                    job.Id,
                    images,
                    visibleNumberTargets,
                    visibleNumberTargets.Min(target => target.MinConfidence)),
                cancellationToken));
        }

        var selectedFaceProviders = SelectProviders(faceProviders, manifest.Providers, options.Value).ToArray();
        foreach (var provider in selectedFaceProviders)
        {
            if (faceTargets.Length == 0)
            {
                continue;
            }

            providerResults.Add(await provider.SearchFacesAsync(
                new ImageBatchFaceSearchRequest(
                    job.Id,
                    images,
                    referenceImages,
                    faceTargets,
                    faceTargets.Min(target => target.MinSimilarity)),
                cancellationToken));
        }

        var matches = providerResults
            .SelectMany(result => result.Matches)
            .GroupBy(item => new { item.TargetKey, item.QueryType, item.ImageKey, item.SourceProvider })
            .Select(group => group
                .OrderByDescending(item => item.Score)
                .First())
            .Select(item => new ImageBatchSearchMatch
            {
                JobId = job.Id,
                ImageAssetId = item.ImageAssetId,
                TargetKey = item.TargetKey,
                QueryType = item.QueryType,
                ImageKey = item.ImageKey,
                ImageFileName = item.ImageFileName,
                Score = item.Score,
                DetectedText = item.DetectedText,
                SourceProvider = item.SourceProvider
            })
            .ToArray();

        var issues = providerResults
            .SelectMany(result => result.Issues)
            .Select(item => new ImageBatchSearchIssue
            {
                JobId = job.Id,
                Code = item.Code,
                Message = item.Message,
                Severity = item.Severity,
                SourceProvider = item.SourceProvider,
                TargetKey = item.TargetKey,
                QueryType = item.QueryType
            })
            .ToArray();

        var providerUris = providerResults
            .SelectMany(result => result.ProviderInputUris)
            .GroupBy(item => item.Key)
            .ToDictionary(group => group.Key, group => group.First().Value);

        await jobRepository.CompleteProcessingAsync(
            job.Id,
            AnalysisJobStatus.Completed.ToString(),
            matches,
            issues,
            providerUris,
            DateTimeOffset.UtcNow,
            cancellationToken);

        logger.LogInformation(ApplicationResourceText.ImageBatchSearchJobProcessingCompletedLogMessage);
    }

    private async Task<ImageBatchSearchManifest> ValidateRequestAsync(
        CreateImageBatchSearchJobCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ClientKey))
        {
            throw new ArgumentException(ApplicationResourceText.ClientKeyRequired, nameof(command));
        }

        if (!string.Equals(command.ProfileKey, ProfileKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(ApplicationResourceText.ImageBatchSearchProfileRequired(ProfileKey), nameof(command));
        }

        var usesCloudImages = command.AssetSelector is not null &&
            !string.Equals(command.AssetSelector.SelectorMode, CloudAssetSelectorModes.MultipartUpload, StringComparison.OrdinalIgnoreCase);
        var usesCloudReferences = command.ReferenceAssetSelector is not null &&
            !string.Equals(command.ReferenceAssetSelector.SelectorMode, CloudAssetSelectorModes.MultipartUpload, StringComparison.OrdinalIgnoreCase);

        if (!usesCloudImages && command.Images.Count == 0)
        {
            throw new ArgumentException(ApplicationResourceText.ImageBatchSearchImagesRequired, nameof(command));
        }

        if (!usesCloudImages && command.Images.Count > options.Value.MaxImagesPerJob)
        {
            throw new ArgumentException(ApplicationResourceText.ImageBatchSearchMaxImagesExceeded(options.Value.MaxImagesPerJob), nameof(command));
        }

        var profile = await profileResolver.ResolveAsync(command.ProfileKey, cancellationToken)
            ?? throw new InvalidOperationException(ApplicationResourceText.ProfileNotFound(command.ProfileKey));

        if (!profile.SupportedAssetTypes.Contains(MediaAssetType.Image))
        {
            throw new InvalidOperationException(ApplicationResourceText.ProfileAssetTypeUnsupported(command.ProfileKey, MediaAssetType.Image));
        }

        var manifest = ImageBatchSearchManifestParser.Parse(command.ManifestJson, options.Value);
        ValidateReferenceImages(manifest, command.ReferenceImages, command.ReferenceAssetSelector, usesCloudReferences);

        return manifest;
    }

    private static void ValidateReferenceImages(
        ImageBatchSearchManifest manifest,
        IReadOnlyCollection<ImageBatchSearchUpload> referenceImages,
        CloudAssetSelector? referenceAssetSelector,
        bool usesCloudReferences)
    {
        var requiredReferenceKeys = manifest.Targets
            .Where(target => string.Equals(target.QueryType, ImageBatchSearchQueryTypes.FaceReference, StringComparison.OrdinalIgnoreCase))
            .Select(target => target.ReferenceImageKey)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requiredReferenceKeys.Length == 0)
        {
            return;
        }

        if (!usesCloudReferences && referenceImages.Count == 0)
        {
            throw new ArgumentException(ApplicationResourceText.ImageBatchSearchReferenceImagesRequired);
        }

        var uploadedKeys = usesCloudReferences
            ? string.Equals(referenceAssetSelector!.SelectorMode, CloudAssetSelectorModes.AllPrefix, StringComparison.OrdinalIgnoreCase)
                ? null
                : referenceAssetSelector.Assets.Select(asset => asset.AssetKey).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : Enumerable.Range(1, referenceImages.Count)
                .Select(index => $"reference-{index}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var referenceKey in requiredReferenceKeys.Where(_ => uploadedKeys is not null))
        {
            if (!uploadedKeys!.Contains(referenceKey!))
            {
                throw new ArgumentException(ApplicationResourceText.ImageBatchSearchTargetReferenceImageMissing(referenceKey!));
            }
        }
    }

    private async Task StoreAssetsAsync(
        ImageBatchSearchJob job,
        IReadOnlyCollection<ImageBatchSearchUpload> uploads,
        string role,
        string keyPrefix,
        CancellationToken cancellationToken)
    {
        var index = 1;
        foreach (var upload in uploads)
        {
            var storedAsset = await storage.SaveAsync(
                upload.Content,
                upload.OriginalFileName,
                upload.ContentType,
                cancellationToken);

            job.Assets.Add(new ImageBatchSearchAsset
            {
                JobId = job.Id,
                AssetKey = $"{keyPrefix}-{index}",
                AssetRole = role,
                OriginalFileName = upload.OriginalFileName,
                StoragePath = storedAsset.StoragePath,
                InputMode = CloudAssetInputModes.MultipartUpload,
                ContentType = storedAsset.ContentType,
                SizeInBytes = storedAsset.SizeInBytes
            });
            index++;
        }
    }

    private async Task AddCloudAssetsAsync(
        ImageBatchSearchJob job,
        CloudAssetSelector selector,
        string role,
        int maxAssets,
        CancellationToken cancellationToken)
    {
        var resolvedAssets = await assetReferenceResolver.ResolveAsync(selector, maxAssets, cancellationToken);
        foreach (var asset in resolvedAssets)
        {
            job.Assets.Add(new ImageBatchSearchAsset
            {
                JobId = job.Id,
                AssetKey = asset.AssetKey,
                AssetRole = role,
                OriginalFileName = asset.FileName,
                StoragePath = ToCloudUri(asset.StorageProvider, asset.BucketName, asset.ObjectKey),
                InputMode = CloudAssetInputModes.CloudReference,
                StorageProvider = asset.StorageProvider,
                BucketName = asset.BucketName,
                ObjectKey = asset.ObjectKey,
                Prefix = selector.Prefix,
                Region = asset.Region,
                Location = asset.Location,
                Checksum = asset.Checksum,
                VersionId = asset.VersionId,
                Generation = asset.Generation,
                ContentType = asset.ContentType,
                SizeInBytes = asset.SizeInBytes
            });
        }
    }

    private static IEnumerable<TProvider> SelectProviders<TProvider>(
        IEnumerable<TProvider> providers,
        IReadOnlyCollection<string> selectedProviderNames,
        ImageBatchSearchOptions options)
        where TProvider : class
    {
        return providers.Where(provider =>
        {
            var sourceProvider = GetSourceProvider(provider);
            var explicitlySelected = selectedProviderNames.Count == 0 ||
                selectedProviderNames.Contains(sourceProvider, StringComparer.OrdinalIgnoreCase);

            return explicitlySelected && IsProviderEnabled(sourceProvider, options);
        });
    }

    private static string GetSourceProvider<TProvider>(TProvider provider)
    {
        return provider switch
        {
            IImageBatchTextSearchProvider textProvider => textProvider.SourceProvider,
            IImageBatchFaceSearchProvider faceProvider => faceProvider.SourceProvider,
            _ => string.Empty
        };
    }

    private static bool IsProviderEnabled(string sourceProvider, ImageBatchSearchOptions options)
    {
        return sourceProvider switch
        {
            "AwsRekognition" => options.AwsProviderEnabled,
            "GoogleVision" => options.GoogleVisionProviderEnabled,
            _ => true
        };
    }

    private static ImageBatchProviderAsset ToProviderAsset(ImageBatchSearchAsset asset)
    {
        return new ImageBatchProviderAsset(
            asset.Id,
            asset.AssetKey,
            asset.OriginalFileName,
            asset.StoragePath,
            asset.ContentType,
            asset.StorageProvider,
            asset.BucketName,
            asset.ObjectKey,
            asset.VersionId,
            asset.Generation);
    }

    private static string ToCloudUri(string storageProvider, string bucketName, string objectKey)
    {
        return string.Equals(storageProvider, CloudStorageProviders.S3, StringComparison.OrdinalIgnoreCase)
            ? $"s3://{bucketName}/{objectKey}"
            : $"gs://{bucketName}/{objectKey}";
    }

    private static ImageBatchSearchResultResponse MapResult(ImageBatchSearchJob job, ImageBatchSearchOptions options)
    {
        var manifest = ImageBatchSearchManifestParser.Parse(job.ManifestJson, options);

        var results = manifest.Targets
            .Select(target => new ImageBatchSearchTargetResultResponse(
                target.TargetKey,
                target.QueryType,
                job.Matches
                    .Where(item => string.Equals(item.TargetKey, target.TargetKey, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(item.QueryType, target.QueryType, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(item => item.ImageKey)
                    .ThenByDescending(item => item.Score)
                    .Select(item => new ImageBatchSearchMatchResponse(
                        item.ImageKey,
                        item.ImageFileName,
                        item.Score,
                        item.DetectedText,
                        item.SourceProvider))
                    .ToArray()))
            .ToArray();

        return new ImageBatchSearchResultResponse(
            job.Id,
            job.ClientKey,
            job.Status,
            results,
            job.Issues
                .Select(item => new AnalysisIssueResponse(item.Code, item.Message, item.Severity, item.SourceProvider))
                .ToArray(),
            job.CreatedAt,
            job.CompletedAt);
    }

    private static ImageBatchSearchJobResponse MapJob(ImageBatchSearchJob job)
    {
        return new ImageBatchSearchJobResponse(
            job.Id,
            job.ClientKey,
            job.ProfileKey,
            job.Status,
            job.ProcessingAttempts,
            job.Assets.Count(asset => string.Equals(asset.AssetRole, SourceImageRole, StringComparison.OrdinalIgnoreCase)),
            job.Assets.Count(asset => string.Equals(asset.AssetRole, ReferenceImageRole, StringComparison.OrdinalIgnoreCase)),
            job.ErrorMessage,
            job.CreatedAt,
            job.CompletedAt);
    }
}
