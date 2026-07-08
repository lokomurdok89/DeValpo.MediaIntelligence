using Amazon.Rekognition.Model;
using DeValpo.MediaIntelligence.Application.ImageBatchSearch;
using DeValpo.MediaIntelligence.Application.ImageBatchSearch.Models;
using DeValpo.MediaIntelligence.Application.VideoSearch;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition.Mappers;

internal static class AwsRekognitionImageBatchSearchMapper
{
    /// <summary>
    /// Converts Rekognition text detections to domain matches without exposing full OCR output.
    /// </summary>
    public static IReadOnlyCollection<ImageBatchProviderMatch> ToDomainTextMatches(
        ImageBatchProviderAsset image,
        DetectTextResponse response,
        IReadOnlyCollection<ImageBatchSearchTarget> targets,
        string sourceProvider)
    {
        var matches = new List<ImageBatchProviderMatch>();
        foreach (var textDetection in response.TextDetections)
        {
            if (!string.Equals(textDetection.Type, "LINE", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(textDetection.Type, "WORD", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var normalizedText = VideoSearchTextNormalizer.NormalizeNumber(textDetection.DetectedText ?? string.Empty);
            if (normalizedText.Length == 0)
            {
                continue;
            }

            foreach (var target in targets)
            {
                var normalizedTarget = VideoSearchTextNormalizer.NormalizeNumber(target.VisibleNumber ?? string.Empty);
                var confidence = ToDecimalConfidence(textDetection.Confidence ?? 0f);
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
                    TruncateDetectedText(textDetection.DetectedText),
                    sourceProvider));
            }
        }

        return matches;
    }

    /// <summary>
    /// Converts Rekognition face matches to target-key image matches without returning FaceId values.
    /// </summary>
    public static IReadOnlyCollection<ImageBatchProviderMatch> ToDomainFaceMatches(
        SearchFacesByImageResponse response,
        ImageBatchSearchTarget target,
        IReadOnlyDictionary<string, ImageBatchProviderAsset> indexedImagesByFaceId,
        string sourceProvider)
    {
        return response.FaceMatches
            .Where(item => item.Face?.FaceId is not null &&
                indexedImagesByFaceId.ContainsKey(item.Face.FaceId))
            .Select(item =>
            {
                var image = indexedImagesByFaceId[item.Face.FaceId];
                return new ImageBatchProviderMatch(
                    image.AssetId,
                    target.TargetKey,
                    ImageBatchSearchQueryTypes.FaceReference,
                    image.AssetKey,
                    image.OriginalFileName,
                    Math.Round((decimal)(item.Similarity ?? 0f), 2),
                    null,
                    sourceProvider);
            })
            .ToArray();
    }

    private static decimal ToDecimalConfidence(float confidence)
    {
        return Math.Round((decimal)confidence / 100m, 4);
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
}
