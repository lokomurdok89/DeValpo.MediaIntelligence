using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Contracts.HumanReview;

public sealed record CompleteHumanReviewTaskRequest(
    string Decision,
    string? Comment,
    IReadOnlyCollection<ReviewFieldCorrectionRequest>? CorrectedFields);
