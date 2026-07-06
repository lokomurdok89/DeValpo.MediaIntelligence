namespace DeValpo.MediaIntelligence.Infrastructure.Webhooks;

public interface IWebhookSignatureService
{
    /// <summary>
    /// Computes the webhook HMAC signature for the timestamp and payload.
    /// </summary>
    string Sign(string signingSecret, string timestamp, string payloadJson);
}
