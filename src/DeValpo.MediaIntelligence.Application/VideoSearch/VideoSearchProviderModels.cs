namespace DeValpo.MediaIntelligence.Application.VideoSearch;

public sealed record VideoSearchProviderRequest(
    Guid JobId,
    IReadOnlyCollection<VideoSearchProviderAsset> Videos,
    bool DetectFaces,
    bool DetectNumbers);

public sealed record VideoSearchProviderAsset(
    Guid AssetId,
    string VideoKey,
    string OriginalFileName,
    string StoragePath,
    string ContentType);

public sealed record VideoSearchProviderResult(
    IReadOnlyCollection<VideoTextOccurrence> TextOccurrences,
    IReadOnlyCollection<VideoFaceOccurrence> FaceOccurrences,
    IReadOnlyCollection<VideoProviderIssue> Issues,
    IReadOnlyDictionary<Guid, string> ProviderInputUris,
    IReadOnlyDictionary<Guid, decimal> VideoDurations);

public sealed record VideoTextOccurrence(
    Guid AssetId,
    string VideoKey,
    string VideoFileName,
    string Text,
    decimal StartTimeSeconds,
    decimal EndTimeSeconds,
    decimal Confidence,
    string SourceProvider);

public sealed record VideoFaceOccurrence(
    Guid AssetId,
    string VideoKey,
    string VideoFileName,
    decimal StartTimeSeconds,
    decimal EndTimeSeconds,
    decimal Confidence,
    string SourceProvider);

public sealed record VideoProviderIssue(
    string Code,
    string Message,
    string Severity,
    string SourceProvider);
