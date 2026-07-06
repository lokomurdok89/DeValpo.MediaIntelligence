namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class AnalysisJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ClientKey { get; set; } = string.Empty;
    public string ProfileKey { get; set; } = string.Empty;
    public string AssetType { get; set; } = "Unknown";
    public string Status { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public int ProcessingAttempts { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public List<MediaAsset> MediaAssets { get; set; } = [];
    public List<SkillExecution> SkillExecutions { get; set; } = [];
    public AnalysisResult? AnalysisResult { get; set; }
    public List<HumanReviewTask> HumanReviewTasks { get; set; } = [];
}
