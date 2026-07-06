namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IWebhookDeliveryProcessor
{
    /// <summary>
    /// Processes one due webhook delivery when one is available.
    /// </summary>
    Task<bool> ProcessNextDueAsync(CancellationToken cancellationToken);
}
