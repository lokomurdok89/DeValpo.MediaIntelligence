namespace DeValpo.MediaIntelligence.Application.FaceSearch.Models;

/// <summary>
/// Provider-neutral request used by infrastructure adapters to execute a face search job.
/// </summary>
public sealed record FaceSearchProviderRequest(
    Guid JobId,
    string ConsentReferenceId,
    IReadOnlyCollection<FaceSearchProviderAsset> Videos,
    IReadOnlyCollection<FaceSearchProviderAsset> ReferenceImages,
    IReadOnlyCollection<FaceSearchTarget> Targets,
    decimal MinSimilarity);

/// <summary>
/// Provider-neutral media asset reference stored by the application before external processing.
/// </summary>
public sealed record FaceSearchProviderAsset(
    Guid AssetId,
    string AssetKey,
    string OriginalFileName,
    string StoragePath,
    string ContentType,
    string? StorageProvider = null,
    string? BucketName = null,
    string? ObjectKey = null,
    string? VersionId = null,
    string? Generation = null);

/// <summary>
/// Provider-neutral result returned by face search infrastructure adapters.
/// </summary>
public sealed record FaceSearchProviderResult(
    IReadOnlyCollection<FaceReferenceMatchOccurrence> FaceMatches,
    IReadOnlyCollection<FaceSearchProviderIssue> Issues,
    IReadOnlyDictionary<Guid, string> ProviderInputUris,
    IReadOnlyDictionary<Guid, decimal> VideoDurations);

/// <summary>
/// Canonical face match occurrence used before persistence and API DTO projection.
/// </summary>
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

/// <summary>
/// Provider-neutral warning or error produced while running a face search job.
/// </summary>
public sealed record FaceSearchProviderIssue(
    string Code,
    string Message,
    string Severity,
    string SourceProvider);
