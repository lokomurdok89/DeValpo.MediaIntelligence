namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class AnalysisResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public AnalysisJob? Job { get; set; }
    public decimal Confidence { get; set; }
    public bool RequiresHumanReview { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<ExtractedField> ExtractedFields { get; set; } = [];
    public List<DetectedEntity> DetectedEntities { get; set; } = [];
    public List<DetectedLabel> DetectedLabels { get; set; } = [];
    public List<GeneratedClip> GeneratedClips { get; set; } = [];
    public List<AnalysisIssue> Issues { get; set; } = [];
}
