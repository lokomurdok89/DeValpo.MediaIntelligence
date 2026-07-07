using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Repositories;

public sealed class AuditEventRepository(MediaIntelligenceDbContext dbContext) : IAuditEventRepository
{
    /// <summary>
    /// Adds an audit event to the current persistence context.
    /// </summary>
    public async Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        await dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
    }

    /// <summary>
    /// Lists recent audit events by optional client and event type filters.
    /// </summary>
    public async Task<IReadOnlyCollection<AuditEvent>> ListAsync(
        string? clientKey,
        string? eventType,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = dbContext.AuditEvents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(clientKey))
        {
            query = query.Where(item => item.ResourceClientKey == clientKey || item.ActorClientKey == clientKey);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(item => item.EventType == eventType);
        }

        return await query
            .OrderByDescending(item => item.CreatedAt)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
    }
}
