using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IClientRepository
{
    Task<Client?> GetByKeyAsync(string clientKey, CancellationToken cancellationToken);
}
