namespace DeValpo.MediaIntelligence.Application.Audit;

public sealed record AuditEventRequest(
    string EventType,
    string? ActorSubject,
    string? ActorClientKey,
    string? ResourceType,
    string? ResourceId,
    string? ResourceClientKey,
    string Outcome,
    IReadOnlyDictionary<string, string>? Metadata);
