namespace DeValpo.MediaIntelligence.Contracts.Webhooks;

public sealed record CreateWebhookTargetRequest(
    string TargetKey,
    string Url,
    string SigningSecret,
    bool IsActive = true);
