using DeValpo.MediaIntelligence.Application.Abstractions;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence;

public sealed class EfUnitOfWork(MediaIntelligenceDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
