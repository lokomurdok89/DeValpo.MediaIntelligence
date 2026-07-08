using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.CloudAssets;
using DeValpo.MediaIntelligence.Application.ImageBatchSearch;
using DeValpo.MediaIntelligence.Application.ImageBatchSearch.Models;
using DeValpo.MediaIntelligence.Application.VideoSearch;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using Google.Cloud.Vision.V1;
using Google.Api.Gax.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Google.Vision;

public sealed class GoogleVisionImageBatchSearchProvider(
    IMediaAssetStorage mediaAssetStorage,
    ICloudAssetReader cloudAssetReader,
    IOptions<GoogleVisionOptions> options) : IImageBatchTextSearchProvider, IImageBatchFaceSearchProvider
{
    public string SourceProvider => "GoogleVision";

    /// <summary>
    /// Runs Google Vision OCR over each uploaded image and maps visible-number matches.
    /// </summary>
    public async Task<ImageBatchProviderResult> SearchTextAsync(
        ImageBatchTextSearchRequest request,
        CancellationToken cancellationToken)
    {
        var config = options.Value;
        if (!config.Enabled)
        {
            return new ImageBatchProviderResult(
                [],
                [new ImageBatchProviderIssue("GOOGLE_VISION_DISABLED", InfrastructureResourceText.GoogleVisionDisabled, "Warning", SourceProvider)],
                new Dictionary<Guid, string>());
        }

        var credential = await GoogleVisionCredentialProvider.GetCredentialAsync(config, cancellationToken);
        var client = await new ImageAnnotatorClientBuilder
        {
            GoogleCredential = credential
        }.BuildAsync(cancellationToken);

        var matches = new List<ImageBatchProviderMatch>();
        var issues = new List<ImageBatchProviderIssue>();

        foreach (var imageAsset in request.Images)
        {
            try
            {
                await using var stream = await OpenAssetReadStreamAsync(imageAsset, cancellationToken);
                var image = Image.FromStream(stream);
                var annotations = await client.DetectTextAsync(
                    image,
                    callSettings: CallSettings.FromCancellationToken(cancellationToken));
                matches.AddRange(ToDomainTextMatches(imageAsset, annotations, request.Targets));
            }
            catch (Exception exception)
            {
                issues.Add(new ImageBatchProviderIssue(
                    "GOOGLE_VISION_OCR_FAILED",
                    ClassifyProviderError(exception),
                    "Error",
                    SourceProvider));
            }
        }

        return new ImageBatchProviderResult(matches, issues, new Dictionary<Guid, string>());
    }

    /// <summary>
    /// Returns Unsupported because Google Vision does not identify specific people by reference image.
    /// </summary>
    public Task<ImageBatchProviderResult> SearchFacesAsync(
        ImageBatchFaceSearchRequest request,
        CancellationToken cancellationToken)
    {
        var issues = request.Targets
            .Select(target => new ImageBatchProviderIssue(
                "GOOGLE_VISION_FACE_REFERENCE_UNSUPPORTED",
                InfrastructureResourceText.GoogleVisionFaceReferenceUnsupported,
                "Unsupported",
                SourceProvider,
                target.TargetKey,
                ImageBatchSearchQueryTypes.FaceReference))
            .ToArray();

        return Task.FromResult(new ImageBatchProviderResult([], issues, new Dictionary<Guid, string>()));
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

    private IReadOnlyCollection<ImageBatchProviderMatch> ToDomainTextMatches(
        ImageBatchProviderAsset image,
        IReadOnlyList<EntityAnnotation> annotations,
        IReadOnlyCollection<ImageBatchSearchTarget> targets)
    {
        var matches = new List<ImageBatchProviderMatch>();
        foreach (var annotation in annotations)
        {
            var normalizedText = VideoSearchTextNormalizer.NormalizeNumber(annotation.Description ?? string.Empty);
            if (normalizedText.Length == 0)
            {
                continue;
            }

            foreach (var target in targets)
            {
                var normalizedTarget = VideoSearchTextNormalizer.NormalizeNumber(target.VisibleNumber ?? string.Empty);
                var confidence = ToDecimalConfidence(annotation.Score);
                if (confidence < target.MinConfidence ||
                    !normalizedText.Contains(normalizedTarget, StringComparison.Ordinal))
                {
                    continue;
                }

                matches.Add(new ImageBatchProviderMatch(
                    image.AssetId,
                    target.TargetKey,
                    ImageBatchSearchQueryTypes.VisibleNumber,
                    image.AssetKey,
                    image.OriginalFileName,
                    confidence,
                    TruncateDetectedText(annotation.Description),
                    SourceProvider));
            }
        }

        return matches;
    }

    private static decimal ToDecimalConfidence(float score)
    {
        return score <= 0f ? 1m : Math.Round((decimal)score, 4);
    }

    private static string? TruncateDetectedText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 128 ? trimmed : trimmed[..128];
    }

    private static string ClassifyProviderError(Exception exception)
    {
        return exception switch
        {
            FileNotFoundException => InfrastructureResourceText.GoogleVisionCredentialsFileNotFound,
            UnauthorizedAccessException => InfrastructureResourceText.GoogleVisionCredentialsCannotBeRead,
            InvalidOperationException => InfrastructureResourceText.GoogleVisionCredentialsInvalid,
            RpcException { StatusCode: StatusCode.Unauthenticated } => InfrastructureResourceText.GoogleVisionCredentialsInvalidOrExpired,
            RpcException { StatusCode: StatusCode.PermissionDenied } => InfrastructureResourceText.GoogleVisionPermissionDenied,
            RpcException { StatusCode: StatusCode.FailedPrecondition } => InfrastructureResourceText.GoogleVisionApiOrBillingUnavailable,
            RpcException { StatusCode: StatusCode.ResourceExhausted } => InfrastructureResourceText.GoogleVisionQuotaReached,
            RpcException { StatusCode: StatusCode.Unavailable } => InfrastructureResourceText.GoogleVisionServiceUnavailable,
            RpcException rpcException => InfrastructureResourceText.GoogleVisionRequestFailedWithStatus(rpcException.StatusCode),
            _ => InfrastructureResourceText.GoogleVisionRequestFailed
        };
    }
}
