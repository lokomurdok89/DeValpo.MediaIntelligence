using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IFaceSearchJobRepository
{
    /// <summary>
    /// Adds a new face search job to the current unit of work.
    /// </summary>
    Task AddAsync(VideoSearchJob job, CancellationToken cancellationToken);

    /// <summary>
    /// Loads the oldest pending AWS face search job with its source and reference assets.
    /// </summary>
    Task<VideoSearchJob?> GetNextPendingWithAssetsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Atomically claims a pending face search job.
    /// </summary>
    Task<bool> MarkRunningAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a face search job graph for processing and API responses.
    /// </summary>
    Task<VideoSearchJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns interrupted running face search jobs to pending.
    /// </summary>
    Task<int> ResetRunningAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Persists retryable or terminal processing failure state.
    /// </summary>
    Task SetProcessingFailureAsync(
        Guid jobId,
        string status,
        string errorMessage,
        DateTimeOffset? completedAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves AWS face reference matches, issues, provider URIs and terminal job status.
    /// </summary>
    Task CompleteProcessingAsync(
        Guid jobId,
        string status,
        IReadOnlyCollection<VideoSearchFaceReferenceMatchSegment> faceMatches,
        IReadOnlyCollection<VideoSearchIssue> issues,
        IReadOnlyDictionary<Guid, string> providerInputUris,
        IReadOnlyDictionary<Guid, decimal> videoDurations,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken);
}
