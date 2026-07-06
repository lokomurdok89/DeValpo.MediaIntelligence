using DeValpo.MediaIntelligence.Application.Models;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IMediaAnalysisOrchestrator
{
    /// <summary>
    /// Executes the skills declared by a profile against the provided media stream.
    /// </summary>
    Task<AnalysisOrchestrationResult> AnalyzeAsync(
        AnalysisOrchestrationRequest request,
        Stream content,
        CancellationToken cancellationToken);
}
