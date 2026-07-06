using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Application.Models;

public sealed record SkillExecutionResult(
    string SkillName,
    decimal Confidence,
    IReadOnlyCollection<ExtractedFieldResponse> ExtractedFields,
    IReadOnlyCollection<DetectedEntityResponse> DetectedEntities,
    IReadOnlyCollection<DetectedLabelResponse> DetectedLabels,
    IReadOnlyCollection<GeneratedClipResponse> GeneratedClips,
    IReadOnlyCollection<AnalysisIssueResponse> Issues);
