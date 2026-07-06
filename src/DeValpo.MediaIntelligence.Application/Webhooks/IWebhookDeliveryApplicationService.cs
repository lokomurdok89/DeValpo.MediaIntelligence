using DeValpo.MediaIntelligence.Contracts.Webhooks;

namespace DeValpo.MediaIntelligence.Application.Webhooks;

public interface IWebhookDeliveryApplicationService
{
    /// <summary>
    /// Registers a webhook target for a client.
    /// </summary>
    Task<WebhookTargetResponse> CreateTargetAsync(
        string clientKey,
        CreateWebhookTargetRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists configured webhook targets for a client.
    /// </summary>
    Task<IReadOnlyCollection<WebhookTargetResponse>> ListTargetsAsync(
        string clientKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists webhook delivery history.
    /// </summary>
    Task<IReadOnlyCollection<WebhookDeliveryResponse>> ListDeliveriesAsync(
        string? clientKey,
        Guid? jobId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates pending webhook deliveries for active targets of the job owner.
    /// </summary>
    Task EnqueueForJobAsync(Guid jobId, string eventType, CancellationToken cancellationToken);
}
