using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Resources;
using DeValpo.MediaIntelligence.Contracts.Retention;
using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Retention;

public sealed class RetentionApplicationService(
    IRetentionRepository repository,
    IMediaAssetStorage storage,
    IUnitOfWork unitOfWork) : IRetentionApplicationService
{
    /// <summary>
    /// Loads the configured retention policy for the client.
    /// </summary>
    public async Task<RetentionPolicyResponse?> GetPolicyAsync(string clientKey, CancellationToken cancellationToken)
    {
        var policy = await repository.GetPolicyAsync(clientKey, cancellationToken);
        return policy is null ? null : Map(policy);
    }

    /// <summary>
    /// Creates or updates the retention policy for the client.
    /// </summary>
    public async Task<RetentionPolicyResponse> UpsertPolicyAsync(
        string clientKey,
        RetentionPolicyRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RetentionDays <= 0)
        {
            throw new ArgumentException(ApplicationResourceText.RetentionDaysMustBePositive, nameof(request));
        }

        var policy = await repository.UpsertPolicyAsync(
            clientKey.Trim(),
            request.Enabled,
            request.RetentionDays,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(policy);
    }

    /// <summary>
    /// Deletes expired stored media files and marks their asset rows as deleted.
    /// </summary>
    public async Task<RetentionExecutionResponse> ExecuteAsync(string clientKey, CancellationToken cancellationToken)
    {
        var policy = await repository.GetPolicyAsync(clientKey.Trim(), cancellationToken)
            ?? throw new InvalidOperationException(ApplicationResourceText.RetentionPolicyNotFound(clientKey));

        if (!policy.Enabled)
        {
            throw new InvalidOperationException(ApplicationResourceText.RetentionPolicyDisabled(clientKey));
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-policy.RetentionDays);
        var assets = await repository.ListExpiredAssetsAsync(clientKey.Trim(), cutoff, cancellationToken);
        var deleted = 0;

        foreach (var asset in assets)
        {
            if (await storage.DeleteAsync(asset.StoragePath, cancellationToken))
            {
                deleted++;
            }

            asset.DeletedAt = DateTimeOffset.UtcNow;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RetentionExecutionResponse(
            clientKey.Trim(),
            assets.Count,
            deleted,
            cutoff,
            DateTimeOffset.UtcNow);
    }

    private static RetentionPolicyResponse Map(ClientRetentionPolicy policy)
    {
        return new RetentionPolicyResponse(
            policy.Client?.ClientKey ?? string.Empty,
            policy.Enabled,
            policy.RetentionDays,
            policy.UpdatedAt);
    }
}
