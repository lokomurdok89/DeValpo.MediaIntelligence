using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Webhooks;

public sealed class WebhookDeliveryProcessor(
    IWebhookDeliveryRepository repository,
    IWebhookDeliverySender sender,
    IOptions<WebhookDeliveryOptions> options) : IWebhookDeliveryProcessor
{
    private const string DeliveredStatus = "Delivered";
    private const string FailedStatus = "Failed";
    private const string RetryPendingStatus = "RetryPending";

    /// <summary>
    /// Sends the oldest due webhook and updates its retry state.
    /// </summary>
    public async Task<bool> ProcessNextDueAsync(CancellationToken cancellationToken)
    {
        var delivery = await repository.GetNextDueAsync(options.Value.MaxAttempts, cancellationToken);
        if (delivery is null)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var result = await sender.SendAsync(delivery, cancellationToken);
        delivery.AttemptCount++;
        delivery.LastAttemptAt = now;
        delivery.LastStatusCode = result.StatusCode;
        delivery.LastError = Truncate(result.ErrorMessage);

        if (result.Success)
        {
            delivery.Status = DeliveredStatus;
            delivery.CompletedAt = DateTimeOffset.UtcNow;
            delivery.NextAttemptAt = null;
        }
        else if (delivery.AttemptCount >= options.Value.MaxAttempts)
        {
            delivery.Status = FailedStatus;
            delivery.CompletedAt = DateTimeOffset.UtcNow;
            delivery.NextAttemptAt = null;
        }
        else
        {
            delivery.Status = RetryPendingStatus;
            delivery.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(CalculateBackoffSeconds(delivery.AttemptCount));
        }

        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private int CalculateBackoffSeconds(int attemptCount)
    {
        var multiplier = Math.Pow(2, Math.Max(attemptCount - 1, 0));
        return (int)Math.Min(options.Value.InitialBackoffSeconds * multiplier, 300);
    }

    private static string? Truncate(string? value)
    {
        return value is { Length: > 2000 } ? value[..2000] : value;
    }
}
