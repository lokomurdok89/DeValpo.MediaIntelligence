using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IAuditEventRepository
{
    /// <summary>
    /// Adds an audit event to the current persistence context.
    /// </summary>
    Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken);

    /// <summary>
    /// Lists recent audit events for review.
    /// </summary>
    Task<IReadOnlyCollection<AuditEvent>> ListAsync(
        string? clientKey,
        string? eventType,
        int limit,
        CancellationToken cancellationToken);
}
