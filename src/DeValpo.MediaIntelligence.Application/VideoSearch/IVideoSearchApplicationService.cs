using DeValpo.MediaIntelligence.Contracts.VideoSearch;

namespace DeValpo.MediaIntelligence.Application.VideoSearch;

public interface IVideoSearchApplicationService
{
    /// <summary>
    /// Stores source videos and creates a pending video search job.
    /// </summary>
    Task<CreateVideoSearchJobResponse> EnqueueAsync(
        CreateVideoSearchJobCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    /// Processes the next pending video search job when one is available.
    /// </summary>
    Task<bool> ProcessNextPendingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Resets interrupted running video jobs after a process restart.
    /// </summary>
    Task<int> ResetRunningJobsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns video search job metadata and current processing state.
    /// </summary>
    Task<VideoSearchJobResponse?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a persisted video search result when the job exists.
    /// </summary>
    Task<VideoSearchResultResponse?> GetResultAsync(Guid jobId, CancellationToken cancellationToken);
}
