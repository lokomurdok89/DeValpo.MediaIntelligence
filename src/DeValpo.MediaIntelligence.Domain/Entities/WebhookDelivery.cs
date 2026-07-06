namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class WebhookDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public Guid? ClientWebhookTargetId { get; set; }
    public ClientWebhookTarget? ClientWebhookTarget { get; set; }
    public string ClientKey { get; set; } = string.Empty;
    public string TargetKey { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastAttemptAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int? LastStatusCode { get; set; }
    public string? LastError { get; set; }
}
