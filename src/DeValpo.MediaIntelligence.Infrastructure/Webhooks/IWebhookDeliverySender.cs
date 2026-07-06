using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Infrastructure.Webhooks;

public interface IWebhookDeliverySender
{
    /// <summary>
    /// Sends one signed webhook request to its configured target.
    /// </summary>
    Task<WebhookSendResult> SendAsync(WebhookDelivery delivery, CancellationToken cancellationToken);
}
