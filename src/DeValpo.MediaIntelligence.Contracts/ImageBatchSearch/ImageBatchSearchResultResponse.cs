using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Contracts.ImageBatchSearch;

public sealed record ImageBatchSearchResultResponse(
    Guid JobId,
    string ClientKey,
    string Status,
    IReadOnlyCollection<ImageBatchSearchTargetResultResponse> Results,
    IReadOnlyCollection<AnalysisIssueResponse> Issues,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
