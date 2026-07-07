using DeValpo.MediaIntelligence.Contracts.FaceSearch;

namespace DeValpo.MediaIntelligence.Application.FaceSearch;

public interface IFaceSearchApplicationService
{
    /// <summary>
    /// Stores source videos and reference images, then creates a pending AWS face search job.
    /// </summary>
    Task<CreateFaceSearchJobResponse> EnqueueAsync(
        CreateFaceSearchJobCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    /// Claims and processes the next pending AWS face search job.
    /// </summary>
    Task<bool> ProcessNextPendingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns interrupted running face search jobs to Pending.
    /// </summary>
    Task<int> ResetRunningJobsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns face search job metadata and current processing state.
    /// </summary>
    Task<FaceSearchJobResponse?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns AWS face reference matches for a completed or running job.
    /// </summary>
    Task<FaceSearchResultResponse?> GetResultAsync(Guid jobId, CancellationToken cancellationToken);
}
