using DeValpo.MediaIntelligence.Application.Models;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IAnalysisProfileResolver
{
    Task<AnalysisProfileDefinition?> ResolveAsync(
        string profileKey,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AnalysisProfileDefinition>> ListAsync(
        CancellationToken cancellationToken);
}
