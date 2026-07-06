using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IWebhookDeliveryRepository
{
    /// <summary>
    /// Adds webhook delivery rows to the current persistence context.
    /// </summary>
    Task AddRangeAsync(IReadOnlyCollection<WebhookDelivery> deliveries, CancellationToken cancellationToken);

    /// <summary>
    /// Lists delivery history filtered by optional client and job.
    /// </summary>
    Task<IReadOnlyCollection<WebhookDelivery>> ListAsync(
        string? clientKey,
        Guid? jobId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads the oldest due delivery that can still be attempted.
    /// </summary>
    Task<WebhookDelivery?> GetNextDueAsync(int maxAttempts, CancellationToken cancellationToken);

    /// <summary>
    /// Commits delivery status changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
