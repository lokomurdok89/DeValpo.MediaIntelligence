namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class ImageBatchSearchAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public ImageBatchSearchJob? Job { get; set; }
    public string AssetKey { get; set; } = string.Empty;
    public string AssetRole { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string InputMode { get; set; } = "MultipartUpload";
    public string? StorageProvider { get; set; }
    public string? BucketName { get; set; }
    public string? ObjectKey { get; set; }
    public string? Prefix { get; set; }
    public string? Region { get; set; }
    public string? Location { get; set; }
    public string? Checksum { get; set; }
    public string? VersionId { get; set; }
    public string? Generation { get; set; }
    public bool IsStaged { get; set; }
    public DateTimeOffset? StagingExpiresAt { get; set; }
    public string? StagingCleanupStatus { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public string? ProviderInputUri { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }
}
