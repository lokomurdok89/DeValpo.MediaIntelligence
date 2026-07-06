namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class SkillExecution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public AnalysisJob? Job { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
    public decimal Confidence { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ErrorMessage { get; set; }
}
