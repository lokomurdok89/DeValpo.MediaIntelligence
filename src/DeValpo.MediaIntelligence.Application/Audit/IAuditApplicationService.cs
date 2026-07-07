using DeValpo.MediaIntelligence.Contracts.Audit;

namespace DeValpo.MediaIntelligence.Application.Audit;

public interface IAuditApplicationService
{
    /// <summary>
    /// Records a sanitized audit event.
    /// </summary>
    Task RecordAsync(AuditEventRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Lists recent audit events for operational review.
    /// </summary>
    Task<IReadOnlyCollection<AuditEventResponse>> ListAsync(
        string? clientKey,
        string? eventType,
        int limit,
        CancellationToken cancellationToken);
}
