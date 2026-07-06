namespace DeValpo.MediaIntelligence.Contracts.AnalysisResults;

public sealed record ExtractedFieldResponse(
    string Name,
    string? Value,
    decimal Confidence,
    string SourceSkill);
