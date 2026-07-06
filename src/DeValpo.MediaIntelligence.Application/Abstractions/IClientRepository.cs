using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IClientRepository
{
    /// <summary>
    /// Loads a client by its stable integration key.
    /// </summary>
    Task<Client?> GetByKeyAsync(string clientKey, CancellationToken cancellationToken);
}
