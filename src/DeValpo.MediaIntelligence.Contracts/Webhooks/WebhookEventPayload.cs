namespace DeValpo.MediaIntelligence.Contracts.Webhooks;

public sealed record WebhookEventPayload(
    Guid EventId,
    string EventType,
    Guid JobId,
    string ClientKey,
    string ProfileKey,
    string JobStatus,
    decimal? Confidence,
    bool? RequiresHumanReview,
    string? ResultUrl,
    DateTimeOffset OccurredAt,
    string? IdempotencyKey = null,
    string? JobType = null,
    string? EventExternalId = null,
    string? AssetSelector = null,
    int Attempt = 0);
