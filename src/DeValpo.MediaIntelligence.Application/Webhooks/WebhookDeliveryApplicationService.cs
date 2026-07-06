using System.Text.Json;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Resources;
using DeValpo.MediaIntelligence.Contracts.Webhooks;
using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Webhooks;

public sealed class WebhookDeliveryApplicationService(
    IClientRepository clientRepository,
    IClientWebhookTargetRepository targetRepository,
    IWebhookDeliveryRepository deliveryRepository,
    IAnalysisJobRepository jobRepository,
    IUnitOfWork unitOfWork) : IWebhookDeliveryApplicationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string ResultUrlTemplate = "/api/v1/analysis-jobs/{0}/result";

    /// <summary>
    /// Registers a webhook target and persists its signing secret for local delivery.
    /// </summary>
    public async Task<WebhookTargetResponse> CreateTargetAsync(
        string clientKey,
        CreateWebhookTargetRequest request,
        CancellationToken cancellationToken)
    {
        ValidateTargetRequest(request);

        var normalizedClientKey = clientKey.Trim();
        var client = await clientRepository.GetByKeyAsync(normalizedClientKey, cancellationToken)
            ?? throw new InvalidOperationException(ApplicationResourceText.ClientNotFound(normalizedClientKey));
        var targetKey = request.TargetKey.Trim();

        if (await targetRepository.ExistsAsync(client.Id, targetKey, cancellationToken))
        {
            throw new InvalidOperationException(ApplicationResourceText.WebhookTargetAlreadyExists(targetKey, normalizedClientKey));
        }

        var target = new ClientWebhookTarget
        {
            ClientId = client.Id,
            TargetKey = targetKey,
            Url = request.Url.Trim(),
            SigningSecret = request.SigningSecret.Trim(),
            IsActive = request.IsActive
        };

        await targetRepository.AddAsync(target, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MapTarget(target, normalizedClientKey);
    }

    /// <summary>
    /// Lists webhook targets configured for a client.
    /// </summary>
    public async Task<IReadOnlyCollection<WebhookTargetResponse>> ListTargetsAsync(
        string clientKey,
        CancellationToken cancellationToken)
    {
        var targets = await targetRepository.ListByClientKeyAsync(clientKey.Trim(), cancellationToken);
        return targets.Select(target => MapTarget(target, clientKey.Trim())).ToArray();
    }

    /// <summary>
    /// Lists webhook delivery history.
    /// </summary>
    public async Task<IReadOnlyCollection<WebhookDeliveryResponse>> ListDeliveriesAsync(
        string? clientKey,
        Guid? jobId,
        CancellationToken cancellationToken)
    {
        var deliveries = await deliveryRepository.ListAsync(clientKey?.Trim(), jobId, cancellationToken);
        return deliveries.Select(MapDelivery).ToArray();
    }

    /// <summary>
    /// Enqueues one delivery per active target for the owner of the completed job.
    /// </summary>
    public async Task EnqueueForJobAsync(Guid jobId, string eventType, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        var targets = await targetRepository.ListActiveByClientKeyAsync(job.ClientKey, cancellationToken);
        if (targets.Count == 0)
        {
            return;
        }

        var occurredAt = DateTimeOffset.UtcNow;
        var deliveries = targets.Select(target =>
        {
            var deliveryId = Guid.NewGuid();
            var payload = new WebhookEventPayload(
                deliveryId,
                eventType,
                job.Id,
                job.ClientKey,
                job.ProfileKey,
                job.Status,
                job.AnalysisResult?.Confidence,
                job.AnalysisResult?.RequiresHumanReview,
                job.AnalysisResult is null ? null : string.Format(System.Globalization.CultureInfo.InvariantCulture, ResultUrlTemplate, job.Id),
                occurredAt);

            return new WebhookDelivery
            {
                Id = deliveryId,
                JobId = job.Id,
                ClientWebhookTargetId = target.Id,
                ClientKey = job.ClientKey,
                TargetKey = target.TargetKey,
                EventType = eventType,
                PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
                Status = "Pending",
                NextAttemptAt = occurredAt
            };
        }).ToArray();

        await deliveryRepository.AddRangeAsync(deliveries, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateTargetRequest(CreateWebhookTargetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetKey))
        {
            throw new ArgumentException(ApplicationResourceText.WebhookTargetKeyRequired, nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            throw new ArgumentException(ApplicationResourceText.WebhookTargetUrlRequired, nameof(request));
        }

        if (!Uri.TryCreate(request.Url.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(ApplicationResourceText.WebhookTargetUrlInvalid, nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SigningSecret))
        {
            throw new ArgumentException(ApplicationResourceText.WebhookSigningSecretRequired, nameof(request));
        }
    }

    private static WebhookTargetResponse MapTarget(ClientWebhookTarget target, string clientKey)
    {
        return new WebhookTargetResponse(
            target.Id,
            target.Client?.ClientKey ?? clientKey,
            target.TargetKey,
            target.Url,
            target.IsActive,
            target.CreatedAt);
    }

    private static WebhookDeliveryResponse MapDelivery(WebhookDelivery delivery)
    {
        return new WebhookDeliveryResponse(
            delivery.Id,
            delivery.JobId,
            delivery.ClientKey,
            delivery.TargetKey,
            delivery.EventType,
            delivery.Status,
            delivery.AttemptCount,
            delivery.CreatedAt,
            delivery.NextAttemptAt,
            delivery.LastAttemptAt,
            delivery.CompletedAt,
            delivery.LastStatusCode,
            delivery.LastError);
    }
}
