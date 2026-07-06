using DeValpo.MediaIntelligence.Application.Models;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IMediaAnalysisOrchestrator
{
    Task<AnalysisOrchestrationResult> AnalyzeAsync(
        AnalysisOrchestrationRequest request,
        Stream content,
        CancellationToken cancellationToken);
}
