namespace DeValpo.MediaIntelligence.Contracts.AnalysisResults;

public sealed record GeneratedClipResponse(
    string ClipKey,
    string? StoragePath,
    decimal Confidence,
    string SourceSkill);
