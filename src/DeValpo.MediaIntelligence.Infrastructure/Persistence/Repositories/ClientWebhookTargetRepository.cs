using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Repositories;

public sealed class ClientWebhookTargetRepository(MediaIntelligenceDbContext dbContext) : IClientWebhookTargetRepository
{
    /// <summary>
    /// Adds a webhook target to the current persistence context.
    /// </summary>
    public async Task AddAsync(ClientWebhookTarget target, CancellationToken cancellationToken)
    {
        await dbContext.ClientWebhookTargets.AddAsync(target, cancellationToken);
    }

    /// <summary>
    /// Lists all webhook targets for the requested client key.
    /// </summary>
    public async Task<IReadOnlyCollection<ClientWebhookTarget>> ListByClientKeyAsync(
        string clientKey,
        CancellationToken cancellationToken)
    {
        return await BaseQuery()
            .Where(target => target.Client!.ClientKey == clientKey)
            .OrderBy(target => target.TargetKey)
            .ToArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Lists active webhook targets for the requested client key.
    /// </summary>
    public async Task<IReadOnlyCollection<ClientWebhookTarget>> ListActiveByClientKeyAsync(
        string clientKey,
        CancellationToken cancellationToken)
    {
        return await BaseQuery()
            .Where(target => target.Client!.ClientKey == clientKey && target.IsActive)
            .OrderBy(target => target.TargetKey)
            .ToArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Loads a webhook target by id.
    /// </summary>
    public Task<ClientWebhookTarget?> GetByIdAsync(Guid targetId, CancellationToken cancellationToken)
    {
        return BaseQuery().FirstOrDefaultAsync(target => target.Id == targetId, cancellationToken);
    }

    /// <summary>
    /// Checks target key uniqueness per client.
    /// </summary>
    public Task<bool> ExistsAsync(Guid clientId, string targetKey, CancellationToken cancellationToken)
    {
        return dbContext.ClientWebhookTargets.AnyAsync(
            target => target.ClientId == clientId && target.TargetKey == targetKey,
            cancellationToken);
    }

    private IQueryable<ClientWebhookTarget> BaseQuery()
    {
        return dbContext.ClientWebhookTargets.Include(target => target.Client);
    }
}
