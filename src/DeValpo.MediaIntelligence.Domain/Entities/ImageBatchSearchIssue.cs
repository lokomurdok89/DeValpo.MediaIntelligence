namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class ImageBatchSearchIssue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public ImageBatchSearchJob? Job { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string SourceProvider { get; set; } = string.Empty;
    public string? TargetKey { get; set; }
    public string? QueryType { get; set; }
}
