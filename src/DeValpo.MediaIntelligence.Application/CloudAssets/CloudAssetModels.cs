namespace DeValpo.MediaIntelligence.Application.CloudAssets;

public sealed record CloudAssetSelector(
    string SelectorMode,
    string StorageProvider,
    string BucketName,
    string? Prefix,
    string? Region,
    string? Location,
    IReadOnlyCollection<CloudAssetReference> Assets);

public sealed record CloudAssetReference(
    string AssetKey,
    string StorageProvider,
    string BucketName,
    string ObjectKey,
    string? Region,
    string? Location,
    string? ContentType,
    long? SizeInBytes,
    string? Checksum,
    string? VersionId,
    string? Generation,
    string? FileName);

public sealed record ResolvedJobAsset(
    string AssetKey,
    string StorageProvider,
    string BucketName,
    string ObjectKey,
    string? Region,
    string? Location,
    string ContentType,
    long SizeInBytes,
    string? Checksum,
    string? VersionId,
    string? Generation,
    string FileName);

public sealed record StagingReference(
    string StorageProvider,
    string BucketName,
    string Prefix,
    Guid JobId,
    DateTimeOffset ExpiresAt,
    string CleanupStatus);
