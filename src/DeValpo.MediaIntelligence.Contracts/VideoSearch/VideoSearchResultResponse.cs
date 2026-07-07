using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Contracts.VideoSearch;

public sealed record VideoSearchResultResponse(
    Guid JobId,
    string ClientKey,
    string Status,
    IReadOnlyCollection<VideoTargetResultResponse> TargetMatches,
    IReadOnlyCollection<VideoFaceOccurrenceSegmentResponse> FaceOccurrences,
    IReadOnlyCollection<AnalysisIssueResponse> Issues,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
