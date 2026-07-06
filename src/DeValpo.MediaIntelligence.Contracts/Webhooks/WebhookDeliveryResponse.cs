namespace DeValpo.MediaIntelligence.Contracts.Webhooks;

public sealed record WebhookDeliveryResponse(
    Guid DeliveryId,
    Guid JobId,
    string ClientKey,
    string TargetKey,
    string EventType,
    string Status,
    int AttemptCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? NextAttemptAt,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? CompletedAt,
    int? LastStatusCode,
    string? LastError);
