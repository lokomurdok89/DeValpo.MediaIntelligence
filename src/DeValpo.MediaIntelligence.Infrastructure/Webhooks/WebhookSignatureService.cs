using System.Security.Cryptography;
using System.Text;

namespace DeValpo.MediaIntelligence.Infrastructure.Webhooks;

public sealed class WebhookSignatureService : IWebhookSignatureService
{
    /// <summary>
    /// Signs the canonical timestamp and JSON payload string with HMAC-SHA256.
    /// </summary>
    public string Sign(string signingSecret, string timestamp, string payloadJson)
    {
        var canonical = $"{timestamp}.{payloadJson}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
