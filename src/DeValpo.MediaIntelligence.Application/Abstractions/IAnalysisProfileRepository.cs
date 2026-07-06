using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IAnalysisProfileRepository
{
    /// <summary>
    /// Lists persisted profile records.
    /// </summary>
    Task<IReadOnlyCollection<AnalysisProfile>> ListAsync(CancellationToken cancellationToken);
}
