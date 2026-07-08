using DeValpo.MediaIntelligence.Application.FaceSearch.Models;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition.Models;

/// <summary>
/// Internal AWS video search result before application persistence.
/// </summary>
internal sealed record AwsFaceSearchVideoResult(
    IReadOnlyCollection<FaceReferenceMatchOccurrence> FaceMatches,
    decimal? DurationSeconds);
