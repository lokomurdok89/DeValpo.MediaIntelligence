namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string? ActorSubject { get; set; }
    public string? ActorClientKey { get; set; }
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public string? ResourceClientKey { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
