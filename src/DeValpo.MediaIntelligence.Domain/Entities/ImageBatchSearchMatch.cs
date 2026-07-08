namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class ImageBatchSearchMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public ImageBatchSearchJob? Job { get; set; }
    public Guid ImageAssetId { get; set; }
    public ImageBatchSearchAsset? ImageAsset { get; set; }
    public string TargetKey { get; set; } = string.Empty;
    public string QueryType { get; set; } = string.Empty;
    public string ImageKey { get; set; } = string.Empty;
    public string ImageFileName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string? DetectedText { get; set; }
    public string SourceProvider { get; set; } = string.Empty;
}
