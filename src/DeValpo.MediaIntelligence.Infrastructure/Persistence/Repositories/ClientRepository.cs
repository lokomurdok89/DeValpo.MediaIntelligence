using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Repositories;

public sealed class ClientRepository(MediaIntelligenceDbContext dbContext) : IClientRepository
{
    /// <summary>
    /// Adds a new client to the current unit of work.
    /// </summary>
    public async Task AddAsync(Client client, CancellationToken cancellationToken)
    {
        await dbContext.Clients.AddAsync(client, cancellationToken);
    }

    /// <summary>
    /// Finds a client by its integration key without tracking the entity.
    /// </summary>
    public Task<Client?> GetByKeyAsync(string clientKey, CancellationToken cancellationToken)
    {
        return dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(client => client.ClientKey == clientKey, cancellationToken);
    }
}
