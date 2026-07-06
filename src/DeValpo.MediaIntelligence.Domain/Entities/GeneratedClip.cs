namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class GeneratedClip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnalysisResultId { get; set; }
    public AnalysisResult? AnalysisResult { get; set; }
    public string ClipKey { get; set; } = string.Empty;
    public string? StoragePath { get; set; }
    public decimal Confidence { get; set; }
    public string SourceSkill { get; set; } = string.Empty;
}
