using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IAnalysisJobRepository
{
    /// <summary>
    /// Adds a new analysis job entity to the persistence context.
    /// </summary>
    Task AddAsync(AnalysisJob job, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a job with its related analysis details when it exists.
    /// </summary>
    Task<AnalysisJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);
}
