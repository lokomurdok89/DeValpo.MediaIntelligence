using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IClientApiKeyRepository
{
    /// <summary>
    /// Adds a hashed client API key to the persistence context.
    /// </summary>
    Task AddAsync(ClientApiKey apiKey, CancellationToken cancellationToken);

    /// <summary>
    /// Loads an API key by its hash including the owning client.
    /// </summary>
    Task<ClientApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a client API key by id including the owning client.
    /// </summary>
    Task<ClientApiKey?> GetByIdAsync(Guid apiKeyId, CancellationToken cancellationToken);
}
