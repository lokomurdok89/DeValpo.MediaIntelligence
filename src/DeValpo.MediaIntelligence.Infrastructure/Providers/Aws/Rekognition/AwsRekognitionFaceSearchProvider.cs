using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.CloudAssets;
using DeValpo.MediaIntelligence.Application.FaceSearch;
using DeValpo.MediaIntelligence.Application.FaceSearch.Models;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition.Mappers;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition.Models;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition;

public sealed class AwsRekognitionFaceSearchProvider(
    IMediaAssetStorage mediaAssetStorage,
    ICloudAssetReader cloudAssetReader,
    IOptions<AwsRekognitionOptions> options,
    IAwsRekognitionClientFactory clientFactory,
    ILogger<AwsRekognitionFaceSearchProvider> logger) : IFaceSearchProvider
{
    private const string SourceProvider = "AwsRekognition";

    /// <summary>
    /// Stages videos and reference images in S3, indexes references and polls Rekognition face search.
    /// </summary>
    public async Task<FaceSearchProviderResult> AnalyzeAsync(
        FaceSearchProviderRequest request,
        CancellationToken cancellationToken)
    {
        var config = options.Value;
        if (!config.Enabled || !config.FaceSearchEnabled)
        {
            return new FaceSearchProviderResult(
                [],
                [new("AWS_REKOGNITION_DISABLED", InfrastructureResourceText.AwsRekognitionAnalysisDisabled, "Warning", SourceProvider)],
                new Dictionary<Guid, string>(),
                new Dictionary<Guid, decimal>());
        }

        using var s3Client = clientFactory.CreateS3Client(config);
        using var rekognitionClient = clientFactory.CreateRekognitionClient(config);

        var providerUris = new Dictionary<Guid, string>();
        var indexedFaceIds = new List<string>();
        var videoDurations = new Dictionary<Guid, decimal>();
        var issues = new List<FaceSearchProviderIssue>();

        try
        {
            var stagedReferenceImages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var referenceImage in request.ReferenceImages)
            {
                var objectKey = await StageAssetAsync(s3Client, config, request.JobId, referenceImage, cancellationToken);
                providerUris[referenceImage.AssetId] = ToS3Uri(config.BucketName, objectKey);
                stagedReferenceImages[referenceImage.AssetKey] = objectKey;
            }

            var targetFaceIds = await IndexTargetFacesAsync(
                rekognitionClient,
                config,
                request.Targets,
                stagedReferenceImages,
                indexedFaceIds,
                cancellationToken);

            foreach (var target in targetFaceIds.Values.Where(item => string.IsNullOrWhiteSpace(item.FaceId)))
            {
                issues.Add(new FaceSearchProviderIssue(
                    "AWS_REKOGNITION_REFERENCE_FACE_NOT_INDEXED",
                    InfrastructureResourceText.AwsRekognitionReferenceFaceNotIndexed(target.ReferenceImageKey),
                    "Warning",
                    SourceProvider));
            }

            var stagedVideos = new Dictionary<FaceSearchProviderAsset, string>();
            foreach (var video in request.Videos)
            {
                var objectKey = await StageAssetAsync(s3Client, config, request.JobId, video, cancellationToken);
                providerUris[video.AssetId] = ToS3Uri(config.BucketName, objectKey);
                stagedVideos[video] = objectKey;
            }

            var faceMatches = new List<FaceReferenceMatchOccurrence>();
            foreach (var item in stagedVideos)
            {
                try
                {
                    var searchResult = await SearchVideoAsync(
                        rekognitionClient,
                        config,
                        request.JobId,
                        item.Key,
                        item.Value,
                        request.MinSimilarity,
                        targetFaceIds,
                        cancellationToken);

                    faceMatches.AddRange(searchResult.FaceMatches);
                    if (searchResult.DurationSeconds.HasValue)
                    {
                        videoDurations[item.Key.AssetId] = searchResult.DurationSeconds.Value;
                    }
                }
                catch (Exception exception)
                {
                    issues.Add(new FaceSearchProviderIssue(
                        "AWS_REKOGNITION_FACE_SEARCH_FAILED",
                        ClassifyProviderError(exception),
                        "Error",
                        SourceProvider));
                }
            }

            return new FaceSearchProviderResult(faceMatches, issues, providerUris, videoDurations);
        }
        finally
        {
            await CleanupAsync(rekognitionClient, config, indexedFaceIds, cancellationToken);
        }
    }

    private async Task<string> StageAssetAsync(
        IAmazonS3 s3Client,
        AwsRekognitionOptions config,
        Guid jobId,
        FaceSearchProviderAsset asset,
        CancellationToken cancellationToken)
    {
        if (string.Equals(asset.StorageProvider, CloudStorageProviders.S3, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(asset.BucketName, config.BucketName, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(asset.ObjectKey))
        {
            return asset.ObjectKey;
        }

        var extension = Path.GetExtension(asset.OriginalFileName);
        var objectKey = $"{NormalizePrefix(config.StagingPrefix)}/face-search/{jobId:N}/{asset.AssetKey}/{Guid.NewGuid():N}{extension}";
        await using var stream = await OpenAssetReadStreamAsync(asset, cancellationToken);
        await s3Client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = config.BucketName,
                Key = objectKey,
                ContentType = asset.ContentType,
                InputStream = stream
            },
            cancellationToken);

        return objectKey;
    }

    private async Task<Stream> OpenAssetReadStreamAsync(
        FaceSearchProviderAsset asset,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(asset.StorageProvider) &&
            !string.IsNullOrWhiteSpace(asset.BucketName) &&
            !string.IsNullOrWhiteSpace(asset.ObjectKey))
        {
            return await cloudAssetReader.OpenReadAsync(
                asset.StorageProvider,
                asset.BucketName,
                asset.ObjectKey,
                asset.VersionId,
                asset.Generation,
                cancellationToken);
        }

        return await mediaAssetStorage.OpenReadAsync(asset.StoragePath, cancellationToken);
    }

    private async Task<Dictionary<string, AwsIndexedTargetFace>> IndexTargetFacesAsync(
        IAmazonRekognition rekognitionClient,
        AwsRekognitionOptions config,
        IReadOnlyCollection<FaceSearchTarget> targets,
        IReadOnlyDictionary<string, string> stagedReferenceImages,
        List<string> indexedFaceIds,
        CancellationToken cancellationToken)
    {
        var indexedTargets = new Dictionary<string, AwsIndexedTargetFace>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targets)
        {
            var response = await rekognitionClient.IndexFacesAsync(
                new IndexFacesRequest
                {
                    CollectionId = config.CollectionId,
                    MaxFaces = 1,
                    QualityFilter = QualityFilter.AUTO,
                    Image = new Image
                    {
                        S3Object = new Amazon.Rekognition.Model.S3Object
                        {
                            Bucket = config.BucketName,
                            Name = stagedReferenceImages[target.ReferenceImageKey]
                        }
                    }
                },
                cancellationToken);

            var faceId = response.FaceRecords.FirstOrDefault()?.Face.FaceId;
            if (string.IsNullOrWhiteSpace(faceId))
            {
                indexedTargets[target.TargetKey] = new AwsIndexedTargetFace(target.TargetKey, target.ReferenceImageKey, string.Empty);
                continue;
            }

            indexedFaceIds.Add(faceId);
            indexedTargets[target.TargetKey] = new AwsIndexedTargetFace(target.TargetKey, target.ReferenceImageKey, faceId);
        }

        return indexedTargets;
    }

    private async Task<AwsFaceSearchVideoResult> SearchVideoAsync(
        IAmazonRekognition rekognitionClient,
        AwsRekognitionOptions config,
        Guid jobId,
        FaceSearchProviderAsset video,
        string videoObjectKey,
        decimal minSimilarity,
        IReadOnlyDictionary<string, AwsIndexedTargetFace> targetFaceIds,
        CancellationToken cancellationToken)
    {
        var indexedTargetsByFaceId = targetFaceIds.Values
            .Where(item => !string.IsNullOrWhiteSpace(item.FaceId))
            .ToDictionary(item => item.FaceId, StringComparer.Ordinal);

        if (indexedTargetsByFaceId.Count == 0)
        {
            return new AwsFaceSearchVideoResult([], null);
        }

        var startResponse = await rekognitionClient.StartFaceSearchAsync(
            new StartFaceSearchRequest
            {
                ClientRequestToken = $"{jobId:N}-{video.AssetKey}",
                CollectionId = config.CollectionId,
                FaceMatchThreshold = (float)minSimilarity,
                JobTag = $"{jobId:N}-{video.AssetKey}",
                Video = new Video
                {
                    S3Object = new Amazon.Rekognition.Model.S3Object
                    {
                        Bucket = config.BucketName,
                        Name = videoObjectKey
                    }
                }
            },
            cancellationToken);

        var deadline = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, config.PollingTimeoutSeconds));
        var delay = TimeSpan.FromSeconds(Math.Max(1, config.PollingIntervalSeconds));
        while (DateTimeOffset.UtcNow < deadline)
        {
            var firstPage = await rekognitionClient.GetFaceSearchAsync(
                new GetFaceSearchRequest
                {
                    JobId = startResponse.JobId,
                    SortBy = FaceSearchSortBy.TIMESTAMP
                },
                cancellationToken);

            if (string.Equals(firstPage.JobStatus, VideoJobStatus.FAILED, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(firstPage.StatusMessage ?? InfrastructureResourceText.AwsRekognitionFaceSearchFailed);
            }

            if (string.Equals(firstPage.JobStatus, VideoJobStatus.SUCCEEDED, StringComparison.OrdinalIgnoreCase))
            {
                return await CollectSucceededResultAsync(
                    rekognitionClient,
                    startResponse.JobId,
                    firstPage,
                    video,
                    indexedTargetsByFaceId,
                    cancellationToken);
            }

            await Task.Delay(delay, cancellationToken);
        }

        throw new TimeoutException(InfrastructureResourceText.AwsRekognitionFaceSearchTimedOut);
    }

    private static async Task<AwsFaceSearchVideoResult> CollectSucceededResultAsync(
        IAmazonRekognition rekognitionClient,
        string jobId,
        GetFaceSearchResponse firstPage,
        FaceSearchProviderAsset video,
        IReadOnlyDictionary<string, AwsIndexedTargetFace> indexedTargetsByFaceId,
        CancellationToken cancellationToken)
    {
        var matches = new List<FaceReferenceMatchOccurrence>();
        decimal? durationSeconds = ToDurationSeconds(firstPage.VideoMetadata);
        var currentPage = firstPage;

        while (true)
        {
            matches.AddRange(AwsRekognitionFaceSearchMapper.ToDomainMatches(
                video,
                currentPage,
                indexedTargetsByFaceId,
                SourceProvider));

            if (string.IsNullOrWhiteSpace(currentPage.NextToken))
            {
                break;
            }

            currentPage = await rekognitionClient.GetFaceSearchAsync(
                new GetFaceSearchRequest
                {
                    JobId = jobId,
                    SortBy = FaceSearchSortBy.TIMESTAMP,
                    NextToken = currentPage.NextToken
                },
                cancellationToken);

            durationSeconds ??= ToDurationSeconds(currentPage.VideoMetadata);
        }

        return new AwsFaceSearchVideoResult(matches, durationSeconds);
    }

    private async Task CleanupAsync(
        IAmazonRekognition rekognitionClient,
        AwsRekognitionOptions config,
        IReadOnlyCollection<string> indexedFaceIds,
        CancellationToken cancellationToken)
    {
        if (indexedFaceIds.Count > 0)
        {
            try
            {
                await rekognitionClient.DeleteFacesAsync(
                    new DeleteFacesRequest
                    {
                        CollectionId = config.CollectionId,
                        FaceIds = indexedFaceIds.ToList()
                    },
                    cancellationToken);
            }
            catch (Exception exception)
            {
                // Cleanup failure must not hide the provider result or original processing error.
                logger.LogWarning(exception, InfrastructureResourceText.AwsRekognitionCleanupFailedFor("indexed faces"));
            }
        }

    }

    private static decimal? ToDurationSeconds(VideoMetadata? metadata)
    {
        return metadata is null
            ? null
            : Math.Round((decimal)metadata.DurationMillis.GetValueOrDefault() / 1000m, 3);
    }

    private static string NormalizePrefix(string prefix)
    {
        var normalized = string.IsNullOrWhiteSpace(prefix) ? "staging" : prefix.Trim();
        return normalized.Trim('/').Replace('\\', '/');
    }

    private static string ToS3Uri(string bucketName, string objectKey)
    {
        return $"s3://{bucketName}/{objectKey}";
    }

    private static string ClassifyProviderError(Exception exception)
    {
        return exception switch
        {
            FileNotFoundException => InfrastructureResourceText.AwsRekognitionCredentialsFileNotFound,
            UnauthorizedAccessException => InfrastructureResourceText.AwsRekognitionCredentialsUnavailable,
            InvalidOperationException { Message: { Length: > 0 } } => exception.Message,
            AmazonRekognitionException { StatusCode: System.Net.HttpStatusCode.Forbidden } => InfrastructureResourceText.AwsRekognitionPermissionDenied,
            AmazonRekognitionException { ErrorCode: "ResourceNotFoundException" } => InfrastructureResourceText.AwsRekognitionCollectionNotFound,
            AmazonS3Exception { StatusCode: System.Net.HttpStatusCode.NotFound } => InfrastructureResourceText.AwsRekognitionBucketNotFound,
            AmazonS3Exception { StatusCode: System.Net.HttpStatusCode.Forbidden } => InfrastructureResourceText.AwsRekognitionPermissionDenied,
            AmazonServiceException { StatusCode: System.Net.HttpStatusCode.Forbidden } => InfrastructureResourceText.AwsRekognitionPermissionDenied,
            AmazonServiceException amazonServiceException => InfrastructureResourceText.AwsRekognitionRemoteCheckFailedWithStatus(amazonServiceException.StatusCode),
            TimeoutException => InfrastructureResourceText.AwsRekognitionFaceSearchTimedOut,
            AmazonClientException => InfrastructureResourceText.AwsRekognitionCredentialsUnavailable,
            _ => InfrastructureResourceText.AwsRekognitionFaceSearchFailed
        };
    }

}
