namespace DeValpo.MediaIntelligence.Contracts.Webhooks;

public sealed record WebhookTargetResponse(
    Guid TargetId,
    string ClientKey,
    string TargetKey,
    string Url,
    bool IsActive,
    DateTimeOffset CreatedAt);
