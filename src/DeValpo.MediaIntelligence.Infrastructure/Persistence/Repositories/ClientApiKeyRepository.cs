using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Repositories;

public sealed class ClientApiKeyRepository(MediaIntelligenceDbContext dbContext) : IClientApiKeyRepository
{
    /// <summary>
    /// Adds a hashed API key to the current unit of work.
    /// </summary>
    public async Task AddAsync(ClientApiKey apiKey, CancellationToken cancellationToken)
    {
        await dbContext.ClientApiKeys.AddAsync(apiKey, cancellationToken);
    }

    /// <summary>
    /// Finds a key by hash and includes the owning client for token issuance.
    /// </summary>
    public Task<ClientApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken)
    {
        return dbContext.ClientApiKeys
            .Include(apiKey => apiKey.Client)
            .FirstOrDefaultAsync(apiKey => apiKey.KeyHash == keyHash, cancellationToken);
    }

    /// <summary>
    /// Finds a key by id and includes the owning client for key lifecycle operations.
    /// </summary>
    public Task<ClientApiKey?> GetByIdAsync(Guid apiKeyId, CancellationToken cancellationToken)
    {
        return dbContext.ClientApiKeys
            .Include(apiKey => apiKey.Client)
            .FirstOrDefaultAsync(apiKey => apiKey.Id == apiKeyId, cancellationToken);
    }
}
