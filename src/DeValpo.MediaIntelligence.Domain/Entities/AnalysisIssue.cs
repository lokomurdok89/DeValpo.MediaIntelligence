namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class AnalysisIssue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnalysisResultId { get; set; }
    public AnalysisResult? AnalysisResult { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string SourceSkill { get; set; } = string.Empty;
}
