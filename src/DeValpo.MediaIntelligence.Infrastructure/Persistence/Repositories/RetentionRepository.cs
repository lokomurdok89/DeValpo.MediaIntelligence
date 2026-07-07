using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Domain.Entities;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using Microsoft.EntityFrameworkCore;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Repositories;

public sealed class RetentionRepository(MediaIntelligenceDbContext dbContext) : IRetentionRepository
{
    private static readonly string[] TerminalStatuses = ["Completed", "Failed", "Cancelled"];

    /// <summary>
    /// Loads the retention policy for a client key.
    /// </summary>
    public Task<ClientRetentionPolicy?> GetPolicyAsync(string clientKey, CancellationToken cancellationToken)
    {
        return dbContext.ClientRetentionPolicies
            .Include(policy => policy.Client)
            .FirstOrDefaultAsync(
                policy => policy.Client != null && policy.Client.ClientKey == clientKey,
                cancellationToken);
    }

    /// <summary>
    /// Creates or updates the retention policy for a client key.
    /// </summary>
    public async Task<ClientRetentionPolicy> UpsertPolicyAsync(
        string clientKey,
        bool enabled,
        int retentionDays,
        CancellationToken cancellationToken)
    {
        var client = await dbContext.Clients
            .Include(item => item.RetentionPolicy)
            .FirstOrDefaultAsync(item => item.ClientKey == clientKey, cancellationToken)
            ?? throw new InvalidOperationException(InfrastructureResourceText.ClientNotFound(clientKey));

        var policy = client.RetentionPolicy;
        if (policy is null)
        {
            policy = new ClientRetentionPolicy
            {
                ClientId = client.Id,
                Client = client
            };
            await dbContext.ClientRetentionPolicies.AddAsync(policy, cancellationToken);
        }

        policy.Enabled = enabled;
        policy.RetentionDays = retentionDays;
        policy.UpdatedAt = DateTimeOffset.UtcNow;
        return policy;
    }

    /// <summary>
    /// Lists assets older than the retention cutoff for terminal jobs.
    /// </summary>
    public async Task<IReadOnlyCollection<MediaAsset>> ListExpiredAssetsAsync(
        string clientKey,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken)
    {
        return await dbContext.MediaAssets
            .Include(asset => asset.Job)
            .Where(asset =>
                asset.DeletedAt == null &&
                asset.CreatedAt <= cutoff &&
                asset.Job != null &&
                asset.Job.ClientKey == clientKey &&
                TerminalStatuses.Contains(asset.Job.Status))
            .OrderBy(asset => asset.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }
}
