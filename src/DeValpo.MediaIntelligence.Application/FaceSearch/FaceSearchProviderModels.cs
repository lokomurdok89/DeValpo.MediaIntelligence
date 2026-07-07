namespace DeValpo.MediaIntelligence.Application.FaceSearch;

public sealed record FaceSearchProviderRequest(
    Guid JobId,
    string ConsentReferenceId,
    IReadOnlyCollection<FaceSearchProviderAsset> Videos,
    IReadOnlyCollection<FaceSearchProviderAsset> ReferenceImages,
    IReadOnlyCollection<FaceSearchTarget> Targets,
    decimal MinSimilarity);

public sealed record FaceSearchProviderAsset(
    Guid AssetId,
    string AssetKey,
    string OriginalFileName,
    string StoragePath,
    string ContentType);

public sealed record FaceSearchProviderResult(
    IReadOnlyCollection<FaceReferenceMatchOccurrence> FaceMatches,
    IReadOnlyCollection<FaceSearchProviderIssue> Issues,
    IReadOnlyDictionary<Guid, string> ProviderInputUris,
    IReadOnlyDictionary<Guid, decimal> VideoDurations);

public sealed record FaceReferenceMatchOccurrence(
    Guid AssetId,
    string TargetKey,
    string ReferenceImageKey,
    string VideoKey,
    string VideoFileName,
    decimal StartTimeSeconds,
    decimal EndTimeSeconds,
    decimal Similarity,
    string SourceProvider);

public sealed record FaceSearchProviderIssue(
    string Code,
    string Message,
    string Severity,
    string SourceProvider);
