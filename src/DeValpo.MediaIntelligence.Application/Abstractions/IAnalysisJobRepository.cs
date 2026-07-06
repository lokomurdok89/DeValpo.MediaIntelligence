using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IAnalysisJobRepository
{
    /// <summary>
    /// Adds a new analysis job entity to the persistence context.
    /// </summary>
    Task AddAsync(AnalysisJob job, CancellationToken cancellationToken);

    /// <summary>
    /// Loads the oldest pending job that already has stored media and can be processed by a worker.
    /// </summary>
    Task<AnalysisJob?> GetNextPendingWithAssetAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Claims a pending job for processing and increments its attempt counter.
    /// </summary>
    Task<bool> MarkRunningAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a job with its related analysis details when it exists.
    /// </summary>
    Task<AnalysisJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Moves jobs left in Running state back to Pending after a process restart.
    /// </summary>
    Task<int> ResetRunningAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Persists a processing failure transition without relying on a tracked job graph.
    /// </summary>
    Task SetProcessingFailureAsync(
        Guid jobId,
        string status,
        string errorMessage,
        DateTimeOffset? completedAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists processing output and moves the job to its terminal success status.
    /// </summary>
    Task CompleteProcessingAsync(
        Guid jobId,
        string status,
        AnalysisResult result,
        IReadOnlyCollection<SkillExecution> skillExecutions,
        IReadOnlyCollection<HumanReviewTask> reviewTasks,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken);
}
