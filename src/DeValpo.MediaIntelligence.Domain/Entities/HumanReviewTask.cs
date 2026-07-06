namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class HumanReviewTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public AnalysisJob? Job { get; set; }
    public string Status { get; set; } = "Pending";
    public string Reason { get; set; } = string.Empty;
    public string? OperatorId { get; set; }
    public string? Decision { get; set; }
    public string? Comment { get; set; }
    public string? OriginalFieldsJson { get; set; }
    public string? CorrectedFieldsJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
