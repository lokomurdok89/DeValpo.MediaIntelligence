using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IAnalysisProfileRepository
{
    Task<IReadOnlyCollection<AnalysisProfile>> ListAsync(CancellationToken cancellationToken);
}
