using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Application.Models;

public sealed record AnalysisOrchestrationResult(
    decimal Confidence,
    bool RequiresHumanReview,
    IReadOnlyCollection<SkillExecutionResult> SkillResults,
    IReadOnlyCollection<AnalysisIssueResponse> Issues);
