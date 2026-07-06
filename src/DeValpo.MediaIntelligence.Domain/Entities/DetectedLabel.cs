namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class DetectedLabel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnalysisResultId { get; set; }
    public AnalysisResult? AnalysisResult { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string SourceSkill { get; set; } = string.Empty;
}
