namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class VideoSearchAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public VideoSearchJob? Job { get; set; }
    public string AssetKey { get; set; } = string.Empty;
    public string AssetRole { get; set; } = "SourceVideo";
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public string? ProviderInputUri { get; set; }
    public decimal? VideoDurationSeconds { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }
}
