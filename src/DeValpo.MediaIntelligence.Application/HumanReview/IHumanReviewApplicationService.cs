using DeValpo.MediaIntelligence.Contracts.HumanReview;

namespace DeValpo.MediaIntelligence.Application.HumanReview;

public interface IHumanReviewApplicationService
{
    /// <summary>
    /// Lists human review tasks filtered by optional status.
    /// </summary>
    Task<IReadOnlyCollection<HumanReviewTaskResponse>> ListAsync(string? status, CancellationToken cancellationToken);

    /// <summary>
    /// Returns one human review task when it exists.
    /// </summary>
    Task<HumanReviewTaskResponse?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>
    /// Moves a pending review task into operator review.
    /// </summary>
    Task<HumanReviewTaskResponse> StartAsync(
        Guid taskId,
        string operatorId,
        StartHumanReviewTaskRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Completes a review task by approving, correcting or rejecting the result.
    /// </summary>
    Task<HumanReviewTaskResponse> CompleteAsync(
        Guid taskId,
        string operatorId,
        CompleteHumanReviewTaskRequest request,
        CancellationToken cancellationToken);
}
