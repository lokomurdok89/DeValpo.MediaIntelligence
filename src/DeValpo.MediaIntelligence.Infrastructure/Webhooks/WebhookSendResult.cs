namespace DeValpo.MediaIntelligence.Infrastructure.Webhooks;

public sealed record WebhookSendResult(
    bool Success,
    int? StatusCode,
    string? ErrorMessage);
