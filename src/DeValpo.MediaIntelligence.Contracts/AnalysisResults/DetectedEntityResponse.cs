namespace DeValpo.MediaIntelligence.Contracts.AnalysisResults;

public sealed record DetectedEntityResponse(
    string EntityType,
    string Value,
    decimal Confidence,
    string SourceSkill);
