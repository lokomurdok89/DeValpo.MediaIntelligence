using System.Text.Json;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.CloudAssets;
using DeValpo.MediaIntelligence.Application.FaceSearch.Models;
using DeValpo.MediaIntelligence.Application.Resources;
using DeValpo.MediaIntelligence.Application.VideoSearch;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Contracts.FaceSearch;
using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Application.FaceSearch;

public sealed class FaceSearchApplicationService(
    IMediaAssetStorage storage,
    IAnalysisProfileResolver profileResolver,
    IFaceSearchJobRepository jobRepository,
    IFaceSearchProvider provider,
    IAssetReferenceResolver assetReferenceResolver,
    IUnitOfWork unitOfWork,
    IOptions<VideoAnalysisOptions> videoOptions,
    IOptions<FaceSearchOptions> faceOptions,
    IOptions<CloudAssetOptions> cloudAssetOptions,
    ILogger<FaceSearchApplicationService> logger) : IFaceSearchApplicationService
{
    private const string ProfileKey = "event-face-search-v1";

    /// <summary>
    /// Stores source videos and reference images, then creates a pending AWS face search job.
    /// </summary>
    public async Task<CreateFaceSearchJobResponse> EnqueueAsync(
        CreateFaceSearchJobCommand command,
        CancellationToken cancellationToken)
    {
        var manifest = await ValidateRequestAsync(command, cancellationToken);
        var job = new VideoSearchJob
        {
            ClientKey = command.ClientKey.Trim(),
            ProfileKey = command.ProfileKey.Trim(),
            Status = AnalysisJobStatus.Pending.ToString(),
            ConsentReferenceId = string.IsNullOrWhiteSpace(command.ConsentReferenceId) ? null : command.ConsentReferenceId.Trim(),
            ManifestJson = JsonSerializer.Serialize(manifest)
        };

        await jobRepository.AddAsync(job, cancellationToken);

        if (command.AssetSelector is not null &&
            !string.Equals(command.AssetSelector.SelectorMode, CloudAssetSelectorModes.MultipartUpload, StringComparison.OrdinalIgnoreCase))
        {
            await AddCloudAssetsAsync(job, command.AssetSelector, "SourceVideo", cloudAssetOptions.Value.MaxVideosPerJob, cancellationToken);
        }
        else
        {
            await StoreAssetsAsync(job, command.Videos, "video", "SourceVideo", cancellationToken);
        }

        if (command.ReferenceAssetSelector is not null &&
            !string.Equals(command.ReferenceAssetSelector.SelectorMode, CloudAssetSelectorModes.MultipartUpload, StringComparison.OrdinalIgnoreCase))
        {
            await AddCloudAssetsAsync(job, command.ReferenceAssetSelector, "ReferenceImage", faceOptions.Value.MaxReferenceImagesPerJob, cancellationToken);
        }
        else
        {
            await StoreAssetsAsync(job, command.ReferenceImages, "reference", "ReferenceImage", cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(ApplicationResourceText.FaceSearchJobQueuedLogMessage);
        return new CreateFaceSearchJobResponse(job.Id, job.Status, job.CreatedAt);
    }

    /// <summary>
    /// Claims and processes the next pending AWS face search job.
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
        catch (Exception exception) when (attempt < videoOptions.Value.MaxProcessingAttempts)
        {
            logger.LogWarning(exception, ApplicationResourceText.FaceSearchJobProcessingRetryLogMessage);
            await jobRepository.SetProcessingFailureAsync(
                job.Id,
                AnalysisJobStatus.Pending.ToString(),
                exception.Message,
                completedAt: null,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, ApplicationResourceText.FaceSearchJobProcessingFailedLogMessage);
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
    /// Returns interrupted running face search jobs to Pending.
    /// </summary>
    public Task<int> ResetRunningJobsAsync(CancellationToken cancellationToken)
    {
        return jobRepository.ResetRunningAsync(cancellationToken);
    }

    /// <summary>
    /// Returns face search job metadata and current processing state.
    /// </summary>
    public async Task<FaceSearchJobResponse?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        return job is null ? null : MapJob(job);
    }

    /// <summary>
    /// Returns AWS face reference matches for a completed or running job.
    /// </summary>
    public async Task<FaceSearchResultResponse?> GetResultAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        return job is null ? null : MapResult(job);
    }

    private async Task ProcessJobAsync(VideoSearchJob job, CancellationToken cancellationToken)
    {
        var manifest = FaceSearchManifestParser.Parse(job.ManifestJson, faceOptions.Value.MinSimilarity);
        var providerResult = await provider.AnalyzeAsync(
            new FaceSearchProviderRequest(
                job.Id,
                job.ConsentReferenceId ?? string.Empty,
                job.Assets.Where(IsSourceVideo).Select(MapProviderAsset).ToArray(),
                job.Assets.Where(IsReferenceImage).Select(MapProviderAsset).ToArray(),
                manifest.Targets,
                manifest.MinSimilarity),
            cancellationToken);

        if (providerResult.VideoDurations.Values.Any(duration => duration > videoOptions.Value.MaxDurationSeconds))
        {
            throw new InvalidOperationException(ApplicationResourceText.VideoSearchMaxDurationExceeded(videoOptions.Value.MaxDurationSeconds));
        }

        var faceMatches = providerResult.FaceMatches.Select(item => new VideoSearchFaceReferenceMatchSegment
        {
            JobId = job.Id,
            VideoAssetId = item.AssetId,
            TargetKey = item.TargetKey,
            ReferenceImageKey = item.ReferenceImageKey,
            VideoKey = item.VideoKey,
            VideoFileName = item.VideoFileName,
            StartTimeSeconds = item.StartTimeSeconds,
            EndTimeSeconds = item.EndTimeSeconds,
            Similarity = item.Similarity,
            SourceProvider = item.SourceProvider
        }).ToArray();

        var issues = providerResult.Issues.Select(item => new VideoSearchIssue
        {
            JobId = job.Id,
            Code = item.Code,
            Message = item.Message,
            Severity = item.Severity,
            SourceProvider = item.SourceProvider
        }).ToArray();

        await jobRepository.CompleteProcessingAsync(
            job.Id,
            AnalysisJobStatus.Completed.ToString(),
            faceMatches,
            issues,
            providerResult.ProviderInputUris,
            providerResult.VideoDurations,
            DateTimeOffset.UtcNow,
            cancellationToken);

        logger.LogInformation(ApplicationResourceText.FaceSearchJobProcessingCompletedLogMessage);
    }

    private async Task<FaceSearchManifest> ValidateRequestAsync(
        CreateFaceSearchJobCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ClientKey))
        {
            throw new ArgumentException(ApplicationResourceText.ClientKeyRequired, nameof(command));
        }

        if (!string.Equals(command.ProfileKey, ProfileKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(ApplicationResourceText.FaceSearchProfileRequired(ProfileKey), nameof(command));
        }

        var usesCloudVideos = command.AssetSelector is not null &&
            !string.Equals(command.AssetSelector.SelectorMode, CloudAssetSelectorModes.MultipartUpload, StringComparison.OrdinalIgnoreCase);
        var usesCloudReferences = command.ReferenceAssetSelector is not null &&
            !string.Equals(command.ReferenceAssetSelector.SelectorMode, CloudAssetSelectorModes.MultipartUpload, StringComparison.OrdinalIgnoreCase);

        if (!usesCloudVideos && command.Videos.Count == 0)
        {
            throw new ArgumentException(ApplicationResourceText.FaceSearchVideosRequired, nameof(command));
        }

        if (!usesCloudVideos && command.Videos.Count > videoOptions.Value.MaxVideosPerJob)
        {
            throw new ArgumentException(ApplicationResourceText.VideoSearchMaxVideosExceeded(videoOptions.Value.MaxVideosPerJob), nameof(command));
        }

        if (!usesCloudReferences && command.ReferenceImages.Count == 0)
        {
            throw new ArgumentException(ApplicationResourceText.FaceSearchReferenceImagesRequired, nameof(command));
        }

        if (!usesCloudReferences && command.ReferenceImages.Count > faceOptions.Value.MaxReferenceImagesPerJob)
        {
            throw new ArgumentException(ApplicationResourceText.VideoSearchMaxVideosExceeded(faceOptions.Value.MaxReferenceImagesPerJob), nameof(command));
        }

        var profile = await profileResolver.ResolveAsync(command.ProfileKey, cancellationToken)
            ?? throw new InvalidOperationException(ApplicationResourceText.ProfileNotFound(command.ProfileKey));

        if (!profile.SupportedAssetTypes.Contains(MediaAssetType.Video))
        {
            throw new InvalidOperationException(ApplicationResourceText.ProfileAssetTypeUnsupported(command.ProfileKey, MediaAssetType.Video));
        }

        if (!profile.SupportedAssetTypes.Contains(MediaAssetType.Image))
        {
            throw new InvalidOperationException(ApplicationResourceText.ProfileAssetTypeUnsupported(command.ProfileKey, MediaAssetType.Image));
        }

        var manifest = FaceSearchManifestParser.Parse(command.ManifestJson, faceOptions.Value.MinSimilarity);
        var referenceImageKeys = usesCloudReferences &&
            string.Equals(command.ReferenceAssetSelector!.SelectorMode, CloudAssetSelectorModes.AllPrefix, StringComparison.OrdinalIgnoreCase)
            ? null
            : usesCloudReferences
            ? command.ReferenceAssetSelector!.Assets.Select(asset => asset.AssetKey).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : Enumerable.Range(1, command.ReferenceImages.Count)
                .Select(index => $"reference-{index}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var target in manifest.Targets.Where(_ => referenceImageKeys is not null))
        {
            if (!referenceImageKeys!.Contains(target.ReferenceImageKey))
            {
                throw new ArgumentException(ApplicationResourceText.FaceSearchTargetReferenceImageMissing(target.ReferenceImageKey), nameof(command));
            }
        }

        return manifest;
    }

    private async Task StoreAssetsAsync(
        VideoSearchJob job,
        IReadOnlyCollection<VideoSearchUpload> uploads,
        string keyPrefix,
        string assetRole,
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

            job.Assets.Add(new VideoSearchAsset
            {
                JobId = job.Id,
                AssetKey = $"{keyPrefix}-{index}",
                AssetRole = assetRole,
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
        VideoSearchJob job,
        CloudAssetSelector selector,
        string role,
        int maxAssets,
        CancellationToken cancellationToken)
    {
        var resolvedAssets = await assetReferenceResolver.ResolveAsync(selector, maxAssets, cancellationToken);
        foreach (var asset in resolvedAssets)
        {
            job.Assets.Add(new VideoSearchAsset
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

    private static FaceSearchProviderAsset MapProviderAsset(VideoSearchAsset asset)
    {
        return new FaceSearchProviderAsset(
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

    private static bool IsSourceVideo(VideoSearchAsset asset)
    {
        return string.Equals(asset.AssetRole, "SourceVideo", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReferenceImage(VideoSearchAsset asset)
    {
        return string.Equals(asset.AssetRole, "ReferenceImage", StringComparison.OrdinalIgnoreCase);
    }

    private static FaceSearchResultResponse MapResult(VideoSearchJob job)
    {
        return new FaceSearchResultResponse(
            job.Id,
            job.ClientKey,
            job.Status,
            job.ConsentReferenceId ?? string.Empty,
            job.FaceReferenceMatchSegments.OrderBy(item => item.VideoKey).ThenBy(item => item.StartTimeSeconds)
                .Select(item => new FaceSearchMatchSegmentResponse(
                    item.TargetKey,
                    item.ReferenceImageKey,
                    item.VideoKey,
                    item.VideoFileName,
                    item.StartTimeSeconds,
                    item.EndTimeSeconds,
                    VideoTimecodeFormatter.Format(item.StartTimeSeconds),
                    VideoTimecodeFormatter.Format(item.EndTimeSeconds),
                    item.Similarity,
                    item.MatchType,
                    item.SourceProvider))
                .ToArray(),
            job.Issues.Select(item => new AnalysisIssueResponse(item.Code, item.Message, item.Severity, item.SourceProvider)).ToArray(),
            job.CreatedAt,
            job.CompletedAt);
    }

    private static FaceSearchJobResponse MapJob(VideoSearchJob job)
    {
        return new FaceSearchJobResponse(
            job.Id,
            job.ClientKey,
            job.ProfileKey,
            job.Status,
            job.ProcessingAttempts,
            job.Assets.Count(IsSourceVideo),
            job.Assets.Count(IsReferenceImage),
            job.ErrorMessage,
            job.CreatedAt,
            job.CompletedAt);
    }
}
