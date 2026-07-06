using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Repositories;

public sealed class ClientRepository(MediaIntelligenceDbContext dbContext) : IClientRepository
{
    public Task<Client?> GetByKeyAsync(string clientKey, CancellationToken cancellationToken)
    {
        return dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(client => client.ClientKey == clientKey, cancellationToken);
    }
}
