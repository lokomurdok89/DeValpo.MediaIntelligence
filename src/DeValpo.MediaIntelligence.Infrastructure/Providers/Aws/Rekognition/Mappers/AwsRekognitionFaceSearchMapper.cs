using Amazon.Rekognition.Model;
using DeValpo.MediaIntelligence.Application.FaceSearch.Models;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition.Models;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition.Mappers;

/// <summary>
/// Maps AWS Rekognition face search responses into provider-neutral application models.
/// </summary>
internal static class AwsRekognitionFaceSearchMapper
{
    /// <summary>
    /// Converts one paged AWS GetFaceSearch response into canonical face match occurrences.
    /// </summary>
    public static IReadOnlyCollection<FaceReferenceMatchOccurrence> ToDomainMatches(
        FaceSearchProviderAsset video,
        GetFaceSearchResponse response,
        IReadOnlyDictionary<string, AwsIndexedTargetFace> indexedTargetsByFaceId,
        string sourceProvider)
    {
        var matches = new List<FaceReferenceMatchOccurrence>();
        foreach (var person in response.Persons)
        {
            var timestampSeconds = Math.Round((decimal)person.Timestamp.GetValueOrDefault() / 1000m, 3);
            foreach (var faceMatch in person.FaceMatches)
            {
                var faceId = faceMatch.Face?.FaceId;
                if (string.IsNullOrWhiteSpace(faceId) ||
                    !indexedTargetsByFaceId.TryGetValue(faceId, out var target))
                {
                    continue;
                }

                matches.Add(new FaceReferenceMatchOccurrence(
                    video.AssetId,
                    target.TargetKey,
                    target.ReferenceImageKey,
                    video.AssetKey,
                    video.OriginalFileName,
                    timestampSeconds,
                    timestampSeconds,
                    Math.Round((decimal)faceMatch.Similarity.GetValueOrDefault(), 2),
                    sourceProvider));
            }
        }

        return matches;
    }
}
