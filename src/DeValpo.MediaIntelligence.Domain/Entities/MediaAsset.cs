namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class MediaAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public AnalysisJob? Job { get; set; }
    public string AssetType { get; set; } = "Unknown";
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
