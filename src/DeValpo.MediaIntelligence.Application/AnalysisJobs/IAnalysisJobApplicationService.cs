using DeValpo.MediaIntelligence.Contracts.AnalysisJobs;

namespace DeValpo.MediaIntelligence.Application.AnalysisJobs;

public interface IAnalysisJobApplicationService
{
    /// <summary>
    /// Creates a pending analysis job after validating the requested profile and asset type.
    /// </summary>
    Task<CreateAnalysisJobResponse> CreateAsync(CreateAnalysisJobRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a persisted analysis job by identifier.
    /// </summary>
    Task<AnalysisJobResponse?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a job that has not reached a terminal status.
    /// </summary>
    Task CancelAsync(Guid jobId, CancellationToken cancellationToken);
}
