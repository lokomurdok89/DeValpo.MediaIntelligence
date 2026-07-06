using DeValpo.MediaIntelligence.Infrastructure.Webhooks;
using FluentAssertions;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class WebhookSignatureServiceTests
{
    [Fact]
    public void Sign_WithKnownPayload_ReturnsExpectedSha256Signature()
    {
        var service = new WebhookSignatureService();
        const string payload = "{\"eventId\":\"11111111-1111-1111-1111-111111111111\",\"eventType\":\"analysis.completed\"}";

        var signature = service.Sign(
            "qa-webhook-secret-min-32-chars",
            "1720000000",
            payload);

        signature.Should().Be("sha256=c87874a4a91030ca7487e7def83c921ecd250cc1ea8e14941995f8f33132fe03");
    }
}
