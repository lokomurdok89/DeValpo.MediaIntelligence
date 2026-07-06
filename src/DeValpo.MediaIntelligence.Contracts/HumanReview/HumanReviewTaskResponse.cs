using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Contracts.HumanReview;

public sealed record HumanReviewTaskResponse(
    Guid TaskId,
    Guid JobId,
    string ClientKey,
    string ProfileKey,
    string Status,
    string Reason,
    string? OperatorId,
    string? Decision,
    string? Comment,
    IReadOnlyCollection<ExtractedFieldResponse> CurrentFields,
    IReadOnlyCollection<ReviewFieldCorrectionRequest> CorrectedFields,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset UpdatedAt);
