namespace DeValpo.MediaIntelligence.Contracts.AnalysisResults;

public sealed record DetectedLabelResponse(
    string Label,
    decimal Confidence,
    string SourceSkill);
