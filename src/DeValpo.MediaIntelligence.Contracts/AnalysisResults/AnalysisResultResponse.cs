namespace DeValpo.MediaIntelligence.Contracts.AnalysisResults;

public sealed record AnalysisResultResponse(
    Guid JobId,
    decimal Confidence,
    bool RequiresHumanReview,
    IReadOnlyCollection<ExtractedFieldResponse> ExtractedFields,
    IReadOnlyCollection<DetectedEntityResponse> DetectedEntities,
    IReadOnlyCollection<DetectedLabelResponse> DetectedLabels,
    IReadOnlyCollection<GeneratedClipResponse> GeneratedClips,
    IReadOnlyCollection<AnalysisIssueResponse> Issues,
    DateTimeOffset CreatedAt);
