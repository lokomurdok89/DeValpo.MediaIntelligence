using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IClientWebhookTargetRepository
{
    /// <summary>
    /// Adds a webhook target to the current persistence context.
    /// </summary>
    Task AddAsync(ClientWebhookTarget target, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all targets configured for the requested client key.
    /// </summary>
    Task<IReadOnlyCollection<ClientWebhookTarget>> ListByClientKeyAsync(string clientKey, CancellationToken cancellationToken);

    /// <summary>
    /// Lists active targets configured for the requested client key.
    /// </summary>
    Task<IReadOnlyCollection<ClientWebhookTarget>> ListActiveByClientKeyAsync(string clientKey, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a target by id with its client metadata.
    /// </summary>
    Task<ClientWebhookTarget?> GetByIdAsync(Guid targetId, CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether a target key already exists for a client.
    /// </summary>
    Task<bool> ExistsAsync(Guid clientId, string targetKey, CancellationToken cancellationToken);
}
