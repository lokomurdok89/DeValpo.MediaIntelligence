using System.Text.Json;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Audit;
using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Audit;

public sealed class AuditApplicationService(
    IAuditEventRepository repository,
    IUnitOfWork unitOfWork) : IAuditApplicationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Records one sanitized audit event.
    /// </summary>
    public async Task RecordAsync(AuditEventRequest request, CancellationToken cancellationToken)
    {
        var auditEvent = new AuditEvent
        {
            EventType = request.EventType,
            ActorSubject = request.ActorSubject,
            ActorClientKey = request.ActorClientKey,
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            ResourceClientKey = request.ResourceClientKey,
            Outcome = request.Outcome,
            MetadataJson = JsonSerializer.Serialize(request.Metadata ?? new Dictionary<string, string>(), JsonOptions)
        };

        await repository.AddAsync(auditEvent, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Lists recent audit events filtered by client and event type.
    /// </summary>
    public async Task<IReadOnlyCollection<AuditEventResponse>> ListAsync(
        string? clientKey,
        string? eventType,
        int limit,
        CancellationToken cancellationToken)
    {
        var events = await repository.ListAsync(clientKey, eventType, Math.Clamp(limit, 1, 500), cancellationToken);
        return events.Select(Map).ToArray();
    }

    private static AuditEventResponse Map(AuditEvent auditEvent)
    {
        return new AuditEventResponse(
            auditEvent.Id,
            auditEvent.EventType,
            auditEvent.ActorSubject,
            auditEvent.ActorClientKey,
            auditEvent.ResourceType,
            auditEvent.ResourceId,
            auditEvent.ResourceClientKey,
            auditEvent.Outcome,
            DeserializeMetadata(auditEvent.MetadataJson),
            auditEvent.CreatedAt);
    }

    private static IReadOnlyDictionary<string, string> DeserializeMetadata(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }
}
