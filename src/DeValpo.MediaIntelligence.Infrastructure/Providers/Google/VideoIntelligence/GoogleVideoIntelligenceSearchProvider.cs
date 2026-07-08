using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.CloudAssets;
using DeValpo.MediaIntelligence.Application.VideoSearch;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using Google.Cloud.Storage.V1;
using Google.Cloud.VideoIntelligence.V1;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Google.VideoIntelligence;

public sealed class GoogleVideoIntelligenceSearchProvider(
    IMediaAssetStorage mediaAssetStorage,
    ICloudAssetReader cloudAssetReader,
    IOptions<GoogleVideoIntelligenceOptions> options) : IVideoSearchProvider
{
    private const string SourceProvider = "GoogleVideoIntelligence";

    /// <summary>
    /// Stages videos in Google Cloud Storage and runs Video Intelligence text and face detection.
    /// </summary>
    public async Task<VideoSearchProviderResult> AnalyzeAsync(
        VideoSearchProviderRequest request,
        CancellationToken cancellationToken)
    {
        var config = options.Value;
        if (!config.Enabled)
        {
            return new VideoSearchProviderResult(
                [],
                [],
                [new("GOOGLE_VIDEO_INTELLIGENCE_DISABLED", InfrastructureResourceText.GoogleVideoIntelligenceDisabled, "Warning", SourceProvider)],
                new Dictionary<Guid, string>(),
                new Dictionary<Guid, decimal>());
        }

        var credential = await GoogleVideoIntelligenceCredentialProvider.GetCredentialAsync(config, cancellationToken);
        var storageClient = await StorageClient.CreateAsync(credential);
        var videoClient = await new VideoIntelligenceServiceClientBuilder
        {
            GoogleCredential = credential
        }.BuildAsync(cancellationToken);

        var textOccurrences = new List<VideoTextOccurrence>();
        var faceOccurrences = new List<VideoFaceOccurrence>();
        var issues = new List<VideoProviderIssue>();
        var providerUris = new Dictionary<Guid, string>();
        var videoDurations = new Dictionary<Guid, decimal>();

        foreach (var asset in request.Videos)
        {
            var inputUri = await StageAssetAsync(storageClient, config.BucketName, request.JobId, asset, cancellationToken);
            providerUris[asset.AssetId] = inputUri;

            try
            {
                var response = await AnnotateVideoAsync(videoClient, inputUri, request, config, cancellationToken);
                MapResponse(asset, response, textOccurrences, faceOccurrences, videoDurations);
            }
            catch (Exception exception)
            {
                issues.Add(new VideoProviderIssue(
                    "GOOGLE_VIDEO_INTELLIGENCE_ANALYSIS_FAILED",
                    ClassifyProviderError(exception),
                    "Error",
                    SourceProvider));
            }
        }

        return new VideoSearchProviderResult(textOccurrences, faceOccurrences, issues, providerUris, videoDurations);
    }

    private async Task<string> StageAssetAsync(
        StorageClient storageClient,
        string bucketName,
        Guid jobId,
        VideoSearchProviderAsset asset,
        CancellationToken cancellationToken)
    {
        if (string.Equals(asset.StorageProvider, CloudStorageProviders.Gcs, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(asset.BucketName, bucketName, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(asset.ObjectKey))
        {
            return $"gs://{bucketName}/{asset.ObjectKey}";
        }

        var extension = Path.GetExtension(asset.OriginalFileName);
        var objectName = $"staging/video-search/{jobId:N}/{asset.VideoKey}/{Guid.NewGuid():N}{extension}";
        await using var stream = await OpenAssetReadStreamAsync(asset, cancellationToken);
        await storageClient.UploadObjectAsync(
            bucketName,
            objectName,
            asset.ContentType,
            stream,
            cancellationToken: cancellationToken);

        return $"gs://{bucketName}/{objectName}";
    }

    private async Task<Stream> OpenAssetReadStreamAsync(
        VideoSearchProviderAsset asset,
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

    private static async Task<AnnotateVideoResponse> AnnotateVideoAsync(
        VideoIntelligenceServiceClient client,
        string inputUri,
        VideoSearchProviderRequest request,
        GoogleVideoIntelligenceOptions config,
        CancellationToken cancellationToken)
    {
        var annotateRequest = new AnnotateVideoRequest
        {
            InputUri = inputUri,
            VideoContext = new VideoContext
            {
                FaceDetectionConfig = new FaceDetectionConfig
                {
                    IncludeBoundingBoxes = true,
                    IncludeAttributes = true
                }
            }
        };

        if (request.DetectNumbers)
        {
            annotateRequest.Features.Add(Feature.TextDetection);
        }

        if (request.DetectFaces)
        {
            annotateRequest.Features.Add(Feature.FaceDetection);
        }

        var operation = await client.AnnotateVideoAsync(annotateRequest, cancellationToken);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(config.RemoteOperationTimeoutSeconds, 1)));

        try
        {
            var completed = await operation.PollUntilCompletedAsync(
                callSettings: global::Google.Api.Gax.Grpc.CallSettings.FromCancellationToken(timeoutCts.Token));
            return completed.Result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(InfrastructureResourceText.GoogleVideoIntelligenceRemoteOperationTimedOut);
        }
    }

    private static void MapResponse(
        VideoSearchProviderAsset asset,
        AnnotateVideoResponse response,
        List<VideoTextOccurrence> textOccurrences,
        List<VideoFaceOccurrence> faceOccurrences,
        Dictionary<Guid, decimal> videoDurations)
    {
        foreach (var result in response.AnnotationResults)
        {
            videoDurations[asset.AssetId] = ToSeconds(result.Segment.EndTimeOffset);

            foreach (var textAnnotation in result.TextAnnotations)
            {
                foreach (var segment in textAnnotation.Segments)
                {
                    textOccurrences.Add(new VideoTextOccurrence(
                        asset.AssetId,
                        asset.VideoKey,
                        asset.OriginalFileName,
                        textAnnotation.Text,
                        ToSeconds(segment.Segment.StartTimeOffset),
                        ToSeconds(segment.Segment.EndTimeOffset),
                        ToDecimalConfidence(segment.Confidence),
                        SourceProvider));
                }
            }

            foreach (var faceAnnotation in result.FaceDetectionAnnotations)
            {
                foreach (var track in faceAnnotation.Tracks)
                {
                    faceOccurrences.Add(new VideoFaceOccurrence(
                        asset.AssetId,
                        asset.VideoKey,
                        asset.OriginalFileName,
                        ToSeconds(track.Segment.StartTimeOffset),
                        ToSeconds(track.Segment.EndTimeOffset),
                        ToDecimalConfidence(track.Confidence),
                        SourceProvider));
                }
            }
        }
    }

    private static decimal ToSeconds(global::Google.Protobuf.WellKnownTypes.Duration duration)
    {
        return Math.Round((decimal)duration.ToTimeSpan().TotalSeconds, 3);
    }

    private static decimal ToDecimalConfidence(float confidence)
    {
        return Math.Round((decimal)confidence, 4);
    }

    private static string ClassifyProviderError(Exception exception)
    {
        return exception switch
        {
            FileNotFoundException => InfrastructureResourceText.GoogleVideoIntelligenceCredentialsFileNotFound,
            UnauthorizedAccessException => InfrastructureResourceText.GoogleVideoIntelligenceCredentialsCannotBeRead,
            InvalidOperationException => InfrastructureResourceText.GoogleVideoIntelligenceCredentialsInvalid,
            RpcException { StatusCode: StatusCode.Unauthenticated } =>
                InfrastructureResourceText.GoogleVideoIntelligenceCredentialsInvalidOrExpired,
            RpcException { StatusCode: StatusCode.PermissionDenied } =>
                InfrastructureResourceText.GoogleVideoIntelligencePermissionDenied,
            RpcException { StatusCode: StatusCode.NotFound } =>
                InfrastructureResourceText.GoogleVideoIntelligenceBucketNotFound,
            RpcException { StatusCode: StatusCode.FailedPrecondition } =>
                InfrastructureResourceText.GoogleVideoIntelligenceApiOrBillingUnavailable,
            RpcException { StatusCode: StatusCode.ResourceExhausted } =>
                InfrastructureResourceText.GoogleVideoIntelligenceQuotaReached,
            RpcException { StatusCode: StatusCode.Unavailable } =>
                InfrastructureResourceText.GoogleVideoIntelligenceServiceUnavailable,
            RpcException rpcException =>
                InfrastructureResourceText.GoogleVideoIntelligenceRequestFailedWithStatus(rpcException.StatusCode),
            TimeoutException => InfrastructureResourceText.GoogleVideoIntelligenceRemoteOperationTimedOut,
            _ => InfrastructureResourceText.GoogleVideoIntelligenceRequestFailed
        };
    }
}
