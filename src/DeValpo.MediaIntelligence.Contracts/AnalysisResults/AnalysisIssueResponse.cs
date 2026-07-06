namespace DeValpo.MediaIntelligence.Contracts.AnalysisResults;

public sealed record AnalysisIssueResponse(
    string Code,
    string Message,
    string Severity,
    string SourceSkill);
