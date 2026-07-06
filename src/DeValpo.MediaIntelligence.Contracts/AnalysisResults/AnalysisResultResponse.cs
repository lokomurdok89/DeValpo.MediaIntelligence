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

public sealed record ExtractedFieldResponse(
    string Name,
    string? Value,
    decimal Confidence,
    string SourceSkill);

public sealed record DetectedEntityResponse(
    string EntityType,
    string Value,
    decimal Confidence,
    string SourceSkill);

public sealed record DetectedLabelResponse(
    string Label,
    decimal Confidence,
    string SourceSkill);

public sealed record GeneratedClipResponse(
    string ClipKey,
    string? StoragePath,
    decimal Confidence,
    string SourceSkill);

public sealed record AnalysisIssueResponse(
    string Code,
    string Message,
    string Severity,
    string SourceSkill);
