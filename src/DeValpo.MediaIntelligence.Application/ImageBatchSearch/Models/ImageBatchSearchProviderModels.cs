namespace DeValpo.MediaIntelligence.Application.ImageBatchSearch.Models;

public sealed record ImageBatchProviderAsset(
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

public sealed record ImageBatchTextSearchRequest(
    Guid JobId,
    IReadOnlyCollection<ImageBatchProviderAsset> Images,
    IReadOnlyCollection<ImageBatchSearchTarget> Targets,
    decimal MinConfidence);

public sealed record ImageBatchFaceSearchRequest(
    Guid JobId,
    IReadOnlyCollection<ImageBatchProviderAsset> Images,
    IReadOnlyCollection<ImageBatchProviderAsset> ReferenceImages,
    IReadOnlyCollection<ImageBatchSearchTarget> Targets,
    decimal MinSimilarity);

public sealed record ImageBatchProviderResult(
    IReadOnlyCollection<ImageBatchProviderMatch> Matches,
    IReadOnlyCollection<ImageBatchProviderIssue> Issues,
    IReadOnlyDictionary<Guid, string> ProviderInputUris);

public sealed record ImageBatchProviderMatch(
    Guid ImageAssetId,
    string TargetKey,
    string QueryType,
    string ImageKey,
    string ImageFileName,
    decimal Score,
    string? DetectedText,
    string SourceProvider);

public sealed record ImageBatchProviderIssue(
    string Code,
    string Message,
    string Severity,
    string SourceProvider,
    string? TargetKey = null,
    string? QueryType = null);
