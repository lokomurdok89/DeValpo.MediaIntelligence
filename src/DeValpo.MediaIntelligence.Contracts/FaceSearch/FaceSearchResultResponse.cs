using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Contracts.FaceSearch;

public sealed record FaceSearchResultResponse(
    Guid JobId,
    string ClientKey,
    string Status,
    string ConsentReferenceId,
    IReadOnlyCollection<FaceSearchMatchSegmentResponse> FaceMatches,
    IReadOnlyCollection<AnalysisIssueResponse> Issues,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
