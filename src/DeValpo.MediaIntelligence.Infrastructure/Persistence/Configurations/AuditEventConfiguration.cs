using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Configurations;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    /// <summary>
    /// Maps sanitized audit trail events.
    /// </summary>
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.EventType).HasMaxLength(150).IsRequired();
        builder.Property(item => item.ActorSubject).HasMaxLength(200);
        builder.Property(item => item.ActorClientKey).HasMaxLength(100);
        builder.Property(item => item.ResourceType).HasMaxLength(100);
        builder.Property(item => item.ResourceId).HasMaxLength(200);
        builder.Property(item => item.ResourceClientKey).HasMaxLength(100);
        builder.Property(item => item.Outcome).HasMaxLength(50).IsRequired();
        builder.Property(item => item.MetadataJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(item => item.CreatedAt);
        builder.HasIndex(item => item.EventType);
        builder.HasIndex(item => item.ResourceClientKey);
    }
}
