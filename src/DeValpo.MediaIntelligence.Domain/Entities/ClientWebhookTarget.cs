namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class ClientWebhookTarget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClientId { get; set; }
    public Client? Client { get; set; }
    public string TargetKey { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SigningSecret { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
