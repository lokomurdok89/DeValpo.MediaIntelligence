using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IHumanReviewTaskRepository
{
    /// <summary>
    /// Lists review tasks with job and result details for operator workflows.
    /// </summary>
    Task<IReadOnlyCollection<HumanReviewTask>> ListAsync(string? status, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a review task with its job and result details.
    /// </summary>
    Task<HumanReviewTask?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces persisted extracted fields for a reviewed result.
    /// </summary>
    Task ReplaceExtractedFieldsAsync(
        Guid analysisResultId,
        IReadOnlyCollection<ExtractedField> correctedFields,
        CancellationToken cancellationToken);
}
