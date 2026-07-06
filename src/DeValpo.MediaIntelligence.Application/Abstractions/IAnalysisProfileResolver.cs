using DeValpo.MediaIntelligence.Application.Models;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IAnalysisProfileResolver
{
    /// <summary>
    /// Resolves one profile definition by its public key.
    /// </summary>
    Task<AnalysisProfileDefinition?> ResolveAsync(
        string profileKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists all profile definitions available for orchestration.
    /// </summary>
    Task<IReadOnlyCollection<AnalysisProfileDefinition>> ListAsync(
        CancellationToken cancellationToken);
}
