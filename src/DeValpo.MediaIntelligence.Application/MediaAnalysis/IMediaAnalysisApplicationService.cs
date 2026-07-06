using DeValpo.MediaIntelligence.Contracts.AnalysisJobs;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Application.MediaAnalysis;

public interface IMediaAnalysisApplicationService
{
    /// <summary>
    /// Stores submitted media and enqueues the job for background processing.
    /// </summary>
    Task<CreateAnalysisJobResponse> EnqueueAsync(
        CreateAnalysisJobRequest request,
        Stream content,
        CancellationToken cancellationToken);

    /// <summary>
    /// Processes the oldest pending job that already has stored media.
    /// </summary>
    Task<bool> ProcessNextPendingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns interrupted running jobs to a retryable pending state.
    /// </summary>
    Task<int> ResetRunningJobsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Loads the analysis result already associated with a job.
    /// </summary>
    Task<AnalysisResultResponse?> GetResultAsync(
        Guid jobId,
        CancellationToken cancellationToken);
}
