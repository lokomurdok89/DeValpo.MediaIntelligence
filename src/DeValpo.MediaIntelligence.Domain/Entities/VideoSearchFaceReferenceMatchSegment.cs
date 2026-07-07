namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class VideoSearchFaceReferenceMatchSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public VideoSearchJob? Job { get; set; }
    public Guid VideoAssetId { get; set; }
    public VideoSearchAsset? VideoAsset { get; set; }
    public string TargetKey { get; set; } = string.Empty;
    public string ReferenceImageKey { get; set; } = string.Empty;
    public string VideoKey { get; set; } = string.Empty;
    public string VideoFileName { get; set; } = string.Empty;
    public decimal StartTimeSeconds { get; set; }
    public decimal EndTimeSeconds { get; set; }
    public decimal Similarity { get; set; }
    public string MatchType { get; set; } = "FaceReference";
    public string SourceProvider { get; set; } = string.Empty;
}
