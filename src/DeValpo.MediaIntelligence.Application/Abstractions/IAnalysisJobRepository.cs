using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IAnalysisJobRepository
{
    Task AddAsync(AnalysisJob job, CancellationToken cancellationToken);
    Task<AnalysisJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);
}
