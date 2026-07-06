using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Repositories;

public sealed class AnalysisProfileRepository(MediaIntelligenceDbContext dbContext) : IAnalysisProfileRepository
{
    /// <summary>
    /// Reads persisted profiles ordered by profile key.
    /// </summary>
    public async Task<IReadOnlyCollection<AnalysisProfile>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.AnalysisProfiles
            .AsNoTracking()
            .OrderBy(profile => profile.ProfileKey)
            .ToArrayAsync(cancellationToken);
    }
}
