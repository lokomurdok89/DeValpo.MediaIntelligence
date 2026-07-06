using DeValpo.MediaIntelligence.Contracts.AnalysisJobs;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Application.MediaAnalysis;

public interface IMediaAnalysisApplicationService
{
    /// <summary>
    /// Stores the submitted media, runs the analysis pipeline and persists the result.
    /// </summary>
    Task<AnalysisResultResponse> AnalyzeAsync(
        CreateAnalysisJobRequest request,
        Stream content,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads the analysis result already associated with a job.
    /// </summary>
    Task<AnalysisResultResponse?> GetResultAsync(
        Guid jobId,
        CancellationToken cancellationToken);
}
