using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IRetentionRepository
{
    /// <summary>
    /// Loads the retention policy configured for a client.
    /// </summary>
    Task<ClientRetentionPolicy?> GetPolicyAsync(string clientKey, CancellationToken cancellationToken);

    /// <summary>
    /// Creates or updates the retention policy for a client.
    /// </summary>
    Task<ClientRetentionPolicy> UpsertPolicyAsync(
        string clientKey,
        bool enabled,
        int retentionDays,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists stored media assets eligible for retention deletion.
    /// </summary>
    Task<IReadOnlyCollection<MediaAsset>> ListExpiredAssetsAsync(
        string clientKey,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken);
}
