using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IImageBatchSearchJobRepository
{
    /// <summary>
    /// Adds a pending image batch search job to the DbContext without saving immediately.
    /// </summary>
    Task AddAsync(ImageBatchSearchJob job, CancellationToken cancellationToken);

    /// <summary>
    /// Loads the oldest pending image batch search job with uploaded asset metadata.
    /// </summary>
    Task<ImageBatchSearchJob?> GetNextPendingWithAssetsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Claims a pending image batch search job for one worker.
    /// </summary>
    Task<bool> MarkRunningAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Loads an image batch search job graph for API responses and worker processing.
    /// </summary>
    Task<ImageBatchSearchJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns interrupted running image batch jobs to Pending.
    /// </summary>
    Task<int> ResetRunningAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Persists a failed or retryable processing transition.
    /// </summary>
    Task SetProcessingFailureAsync(
        Guid jobId,
        string status,
        string errorMessage,
        DateTimeOffset? completedAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves image batch matches, issues, provider URIs and terminal job status in one transaction.
    /// </summary>
    Task CompleteProcessingAsync(
        Guid jobId,
        string status,
        IReadOnlyCollection<ImageBatchSearchMatch> matches,
        IReadOnlyCollection<ImageBatchSearchIssue> issues,
        IReadOnlyDictionary<Guid, string> providerInputUris,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken);
}
