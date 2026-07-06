using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Repositories;

public sealed class WebhookDeliveryRepository(MediaIntelligenceDbContext dbContext) : IWebhookDeliveryRepository
{
    private static readonly string[] ProcessableStatuses = ["Pending", "RetryPending"];

    /// <summary>
    /// Adds delivery records to the current persistence context.
    /// </summary>
    public async Task AddRangeAsync(IReadOnlyCollection<WebhookDelivery> deliveries, CancellationToken cancellationToken)
    {
        await dbContext.WebhookDeliveries.AddRangeAsync(deliveries, cancellationToken);
    }

    /// <summary>
    /// Lists delivery history by optional client and job filters.
    /// </summary>
    public async Task<IReadOnlyCollection<WebhookDelivery>> ListAsync(
        string? clientKey,
        Guid? jobId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.WebhookDeliveries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(clientKey))
        {
            query = query.Where(delivery => delivery.ClientKey == clientKey);
        }

        if (jobId is not null)
        {
            query = query.Where(delivery => delivery.JobId == jobId);
        }

        return await query
            .OrderByDescending(delivery => delivery.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Returns the oldest due webhook delivery that has attempts remaining.
    /// </summary>
    public Task<WebhookDelivery?> GetNextDueAsync(int maxAttempts, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        return dbContext.WebhookDeliveries
            .Include(delivery => delivery.ClientWebhookTarget)
            .Where(delivery =>
                ProcessableStatuses.Contains(delivery.Status) &&
                delivery.AttemptCount < maxAttempts &&
                (delivery.NextAttemptAt == null || delivery.NextAttemptAt <= now))
            .OrderBy(delivery => delivery.NextAttemptAt ?? delivery.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Saves delivery status updates.
    /// </summary>
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
