namespace DeValpo.MediaIntelligence.Contracts.Audit;

public sealed record AuditEventResponse(
    Guid EventId,
    string EventType,
    string? ActorSubject,
    string? ActorClientKey,
    string? ResourceType,
    string? ResourceId,
    string? ResourceClientKey,
    string Outcome,
    IReadOnlyDictionary<string, string> Metadata,
    DateTimeOffset CreatedAt);
