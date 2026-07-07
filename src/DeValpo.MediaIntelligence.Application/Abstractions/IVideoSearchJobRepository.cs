using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IVideoSearchJobRepository
{
    /// <summary>
    /// Adds a pending video search job with its uploaded asset metadata.
    /// </summary>
    Task AddAsync(VideoSearchJob job, CancellationToken cancellationToken);

    /// <summary>
    /// Loads the oldest pending video search job that has at least one source video.
    /// </summary>
    Task<VideoSearchJob?> GetNextPendingWithAssetsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Claims a pending video search job for one worker.
    /// </summary>
    Task<bool> MarkRunningAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a video search job with assets, segments and issues for API responses.
    /// </summary>
    Task<VideoSearchJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns interrupted running video jobs to Pending.
    /// </summary>
    Task<int> ResetRunningAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Persists a failure transition for a video job.
    /// </summary>
    Task SetProcessingFailureAsync(
        Guid jobId,
        string status,
        string errorMessage,
        DateTimeOffset? completedAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists video search output and moves the job to its terminal success status.
    /// </summary>
    Task CompleteProcessingAsync(
        Guid jobId,
        string status,
        IReadOnlyCollection<VideoSearchTargetMatchSegment> targetSegments,
        IReadOnlyCollection<VideoSearchFaceOccurrenceSegment> faceSegments,
        IReadOnlyCollection<VideoSearchIssue> issues,
        IReadOnlyDictionary<Guid, string> providerInputUris,
        IReadOnlyDictionary<Guid, decimal> videoDurations,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken);
}
