using System.Net.Http.Headers;
using System.Text;
using DeValpo.MediaIntelligence.Domain.Entities;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Webhooks;

public sealed class WebhookDeliverySender(
    HttpClient httpClient,
    IWebhookSignatureService signatureService,
    IOptions<WebhookDeliveryOptions> options) : IWebhookDeliverySender
{
    private const string JsonContentType = "application/json";

    /// <summary>
    /// Sends a signed JSON webhook payload to the target URL.
    /// </summary>
    public async Task<WebhookSendResult> SendAsync(
        WebhookDelivery delivery,
        CancellationToken cancellationToken)
    {
        if (delivery.ClientWebhookTarget is null)
        {
            return new WebhookSendResult(false, null, InfrastructureResourceText.WebhookTargetNotLoaded);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(options.Value.RequestTimeoutSeconds));

        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
            var signature = signatureService.Sign(
                delivery.ClientWebhookTarget.SigningSecret,
                timestamp,
                delivery.PayloadJson);

            using var request = new HttpRequestMessage(HttpMethod.Post, delivery.ClientWebhookTarget.Url)
            {
                Content = new StringContent(delivery.PayloadJson, Encoding.UTF8, JsonContentType)
            };

            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("DeValpo.MediaIntelligence", "1.0"));
            request.Headers.Add("X-DeValpo-Event-Id", delivery.Id.ToString());
            request.Headers.Add("X-DeValpo-Event-Type", delivery.EventType);
            request.Headers.Add("X-DeValpo-Timestamp", timestamp);
            request.Headers.Add("X-DeValpo-Signature", signature);

            using var response = await httpClient.SendAsync(request, timeoutCts.Token);
            var statusCode = (int)response.StatusCode;

            return new WebhookSendResult(
                response.IsSuccessStatusCode,
                statusCode,
                response.IsSuccessStatusCode ? null : response.ReasonPhrase);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new WebhookSendResult(false, null, InfrastructureResourceText.WebhookRequestTimedOut);
        }
        catch (HttpRequestException exception)
        {
            return new WebhookSendResult(false, null, exception.Message);
        }
    }
}
