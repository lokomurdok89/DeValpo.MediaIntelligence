using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.CloudAssets;
using DeValpo.MediaIntelligence.Application.ImageBatchSearch;
using DeValpo.MediaIntelligence.Application.ImageBatchSearch.Models;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition.Mappers;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using S3Object = Amazon.Rekognition.Model.S3Object;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition;

public sealed class AwsRekognitionImageBatchSearchProvider(
    IMediaAssetStorage mediaAssetStorage,
    ICloudAssetReader cloudAssetReader,
    IOptions<AwsRekognitionOptions> options,
    IAwsRekognitionClientFactory clientFactory,
    ILogger<AwsRekognitionImageBatchSearchProvider> logger) : IImageBatchTextSearchProvider, IImageBatchFaceSearchProvider
{
    public string SourceProvider => "AwsRekognition";

    /// <summary>
    /// Runs Rekognition DetectText over each uploaded image and maps visible-number matches.
    /// </summary>
    public async Task<ImageBatchProviderResult> SearchTextAsync(
        ImageBatchTextSearchRequest request,
        CancellationToken cancellationToken)
    {
        var config = options.Value;
        if (!config.Enabled)
        {
            return DisabledResult("AWS_REKOGNITION_DISABLED", InfrastructureResourceText.AwsRekognitionDisabled);
        }

        using var s3Client = clientFactory.CreateS3Client(config);
        using var rekognitionClient = clientFactory.CreateRekognitionClient(config);
        var providerUris = new Dictionary<Guid, string>();
        var matches = new List<ImageBatchProviderMatch>();
        var issues = new List<ImageBatchProviderIssue>();

        try
        {
            foreach (var image in request.Images)
            {
                var objectKey = await StageAssetAsync(s3Client, config, request.JobId, image, cancellationToken);
                providerUris[image.AssetId] = ToS3Uri(config.BucketName, objectKey);

                try
                {
                    var response = await rekognitionClient.DetectTextAsync(
                        new DetectTextRequest
                        {
                            Image = new Image
                            {
                                S3Object = new S3Object
                                {
                                    Bucket = config.BucketName,
                                    Name = objectKey
                                }
                            }
                        },
                        cancellationToken);

                    matches.AddRange(AwsRekognitionImageBatchSearchMapper.ToDomainTextMatches(
                        image,
                        response,
                        request.Targets,
                        SourceProvider));
                }
                catch (Exception exception)
                {
                    issues.Add(new ImageBatchProviderIssue(
                        "AWS_REKOGNITION_DETECT_TEXT_FAILED",
                        ClassifyProviderError(exception),
                        "Error",
                        SourceProvider));
                }
            }

            return new ImageBatchProviderResult(matches, issues, providerUris);
        }
        finally
        {
        }
    }

    /// <summary>
    /// Indexes source-image faces temporarily, searches each reference image and deletes indexed FaceIds.
    /// </summary>
    public async Task<ImageBatchProviderResult> SearchFacesAsync(
        ImageBatchFaceSearchRequest request,
        CancellationToken cancellationToken)
    {
        var config = options.Value;
        if (!config.Enabled || !config.FaceSearchEnabled)
        {
            return DisabledResult("AWS_REKOGNITION_FACE_SEARCH_DISABLED", InfrastructureResourceText.AwsRekognitionFaceSearchDisabled);
        }

        using var s3Client = clientFactory.CreateS3Client(config);
        using var rekognitionClient = clientFactory.CreateRekognitionClient(config);

        var indexedFaceIds = new List<string>();
        var providerUris = new Dictionary<Guid, string>();
        var matches = new List<ImageBatchProviderMatch>();
        var issues = new List<ImageBatchProviderIssue>();

        try
        {
            var indexedImagesByFaceId = await IndexSourceImagesAsync(
                s3Client,
                rekognitionClient,
                config,
                request,
                indexedFaceIds,
                providerUris,
                issues,
                cancellationToken);

            var referenceObjectKeys = await StageReferenceImagesAsync(
                s3Client,
                config,
                request,
                providerUris,
                cancellationToken);

            foreach (var target in request.Targets)
            {
                if (target.ReferenceImageKey is null ||
                    !referenceObjectKeys.TryGetValue(target.ReferenceImageKey, out var referenceObjectKey))
                {
                    issues.Add(new ImageBatchProviderIssue(
                        "AWS_REKOGNITION_REFERENCE_IMAGE_MISSING",
                        InfrastructureResourceText.AwsRekognitionReferenceImageMissing(target.ReferenceImageKey ?? string.Empty),
                        "Warning",
                        SourceProvider,
                        target.TargetKey,
                        ImageBatchSearchQueryTypes.FaceReference));
                    continue;
                }

                try
                {
                    var response = await rekognitionClient.SearchFacesByImageAsync(
                        new SearchFacesByImageRequest
                        {
                            CollectionId = config.CollectionId,
                            FaceMatchThreshold = (float)target.MinSimilarity,
                            Image = new Image
                            {
                                S3Object = new S3Object
                                {
                                    Bucket = config.BucketName,
                                    Name = referenceObjectKey
                                }
                            }
                        },
                        cancellationToken);

                    matches.AddRange(AwsRekognitionImageBatchSearchMapper.ToDomainFaceMatches(
                        response,
                        target,
                        indexedImagesByFaceId,
                        SourceProvider));
                }
                catch (Exception exception)
                {
                    issues.Add(new ImageBatchProviderIssue(
                        "AWS_REKOGNITION_SEARCH_FACES_BY_IMAGE_FAILED",
                        ClassifyProviderError(exception),
                        "Error",
                        SourceProvider,
                        target.TargetKey,
                        ImageBatchSearchQueryTypes.FaceReference));
                }
            }

            return new ImageBatchProviderResult(matches, issues, providerUris);
        }
        finally
        {
            await CleanupFacesAsync(rekognitionClient, config, indexedFaceIds, cancellationToken);
        }
    }

    private async Task<Dictionary<string, ImageBatchProviderAsset>> IndexSourceImagesAsync(
        IAmazonS3 s3Client,
        IAmazonRekognition rekognitionClient,
        AwsRekognitionOptions config,
        ImageBatchFaceSearchRequest request,
        List<string> indexedFaceIds,
        Dictionary<Guid, string> providerUris,
        List<ImageBatchProviderIssue> issues,
        CancellationToken cancellationToken)
    {
        var indexedImagesByFaceId = new Dictionary<string, ImageBatchProviderAsset>(StringComparer.Ordinal);
        foreach (var image in request.Images)
        {
            var objectKey = await StageAssetAsync(s3Client, config, request.JobId, image, cancellationToken);
            providerUris[image.AssetId] = ToS3Uri(config.BucketName, objectKey);

            try
            {
                var response = await rekognitionClient.IndexFacesAsync(
                    new IndexFacesRequest
                    {
                        CollectionId = config.CollectionId,
                        MaxFaces = 20,
                        QualityFilter = QualityFilter.AUTO,
                        Image = new Image
                        {
                            S3Object = new S3Object
                            {
                                Bucket = config.BucketName,
                                Name = objectKey
                            }
                        }
                    },
                    cancellationToken);

                foreach (var faceRecord in response.FaceRecords)
                {
                    var faceId = faceRecord.Face?.FaceId;
                    if (string.IsNullOrWhiteSpace(faceId))
                    {
                        continue;
                    }

                    indexedFaceIds.Add(faceId);
                    indexedImagesByFaceId[faceId] = image;
                }
            }
            catch (Exception exception)
            {
                issues.Add(new ImageBatchProviderIssue(
                    "AWS_REKOGNITION_INDEX_IMAGE_FACE_FAILED",
                    ClassifyProviderError(exception),
                    "Error",
                    SourceProvider));
            }
        }

        if (indexedImagesByFaceId.Count == 0)
        {
            issues.Add(new ImageBatchProviderIssue(
                "AWS_REKOGNITION_NO_SOURCE_FACES_INDEXED",
                InfrastructureResourceText.AwsRekognitionNoSourceFacesIndexed,
                "Warning",
                SourceProvider));
        }

        return indexedImagesByFaceId;
    }

    private async Task<Dictionary<string, string>> StageReferenceImagesAsync(
        IAmazonS3 s3Client,
        AwsRekognitionOptions config,
        ImageBatchFaceSearchRequest request,
        Dictionary<Guid, string> providerUris,
        CancellationToken cancellationToken)
    {
        var referenceObjectKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var referenceImage in request.ReferenceImages)
        {
            var objectKey = await StageAssetAsync(s3Client, config, request.JobId, referenceImage, cancellationToken);
            providerUris[referenceImage.AssetId] = ToS3Uri(config.BucketName, objectKey);
            referenceObjectKeys[referenceImage.AssetKey] = objectKey;
        }

        return referenceObjectKeys;
    }

    private async Task<string> StageAssetAsync(
        IAmazonS3 s3Client,
        AwsRekognitionOptions config,
        Guid jobId,
        ImageBatchProviderAsset asset,
        CancellationToken cancellationToken)
    {
        if (string.Equals(asset.StorageProvider, CloudStorageProviders.S3, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(asset.BucketName, config.BucketName, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(asset.ObjectKey))
        {
            return asset.ObjectKey;
        }

        var extension = Path.GetExtension(asset.OriginalFileName);
        var objectKey = $"{NormalizePrefix(config.StagingPrefix)}/image-batch-search/{jobId:N}/{asset.AssetKey}/{Guid.NewGuid():N}{extension}";
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
        ImageBatchProviderAsset asset,
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

    private async Task CleanupFacesAsync(
        IAmazonRekognition rekognitionClient,
        AwsRekognitionOptions config,
        IReadOnlyCollection<string> indexedFaceIds,
        CancellationToken cancellationToken)
    {
        if (indexedFaceIds.Count == 0)
        {
            return;
        }

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

    private ImageBatchProviderResult DisabledResult(string code, string message)
    {
        return new ImageBatchProviderResult(
            [],
            [new ImageBatchProviderIssue(code, message, "Warning", SourceProvider)],
            new Dictionary<Guid, string>());
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
            AmazonClientException => InfrastructureResourceText.AwsRekognitionCredentialsUnavailable,
            _ => InfrastructureResourceText.AwsRekognitionRemoteCheckFailed
        };
    }
}
