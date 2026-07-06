using DeValpo.MediaIntelligence.Application.Abstractions;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence;

public sealed class EfUnitOfWork(MediaIntelligenceDbContext dbContext) : IUnitOfWork
{
    /// <summary>
    /// Persists all pending Entity Framework changes.
    /// </summary>
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
