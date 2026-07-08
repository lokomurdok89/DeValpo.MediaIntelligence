using DeValpo.MediaIntelligence.Contracts.ImageBatchSearch;

namespace DeValpo.MediaIntelligence.Application.ImageBatchSearch;

public interface IImageBatchSearchApplicationService
{
    /// <summary>
    /// Stores uploaded images and creates a pending image batch search job.
    /// </summary>
    Task<CreateImageBatchSearchJobResponse> EnqueueAsync(
        CreateImageBatchSearchJobCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    /// Claims and processes the next pending image batch search job.
    /// </summary>
    Task<bool> ProcessNextPendingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns interrupted running image batch jobs to Pending.
    /// </summary>
    Task<int> ResetRunningJobsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns image batch job metadata and current processing state.
    /// </summary>
    Task<ImageBatchSearchJobResponse?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns target-grouped image batch search results.
    /// </summary>
    Task<ImageBatchSearchResultResponse?> GetResultAsync(Guid jobId, CancellationToken cancellationToken);
}
