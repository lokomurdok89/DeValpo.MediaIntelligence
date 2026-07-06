namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class HumanReviewTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public AnalysisJob? Job { get; set; }
    public string Status { get; set; } = "Open";
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
