using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    /// <summary>
    /// Maps API clients and enforces unique client keys.
    /// </summary>
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ClientKey).HasMaxLength(100).IsRequired();
        builder.Property(item => item.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.ContactEmail).HasMaxLength(320);
        builder.Property(item => item.AllowedProfileKeysJson).HasColumnType("jsonb").HasDefaultValue("[]").IsRequired();
        builder.HasIndex(item => item.ClientKey).IsUnique();
    }
}

public sealed class ClientWebhookTargetConfiguration : IEntityTypeConfiguration<ClientWebhookTarget>
{
    /// <summary>
    /// Maps webhook targets configured for outbound client notifications.
    /// </summary>
    public void Configure(EntityTypeBuilder<ClientWebhookTarget> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.TargetKey).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Url).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.SigningSecret).HasMaxLength(500).IsRequired();
        builder.HasIndex(item => new { item.ClientId, item.TargetKey }).IsUnique();
        builder.HasOne(item => item.Client).WithMany(client => client.WebhookTargets).HasForeignKey(item => item.ClientId);
    }
}

public sealed class ClientRetentionPolicyConfiguration : IEntityTypeConfiguration<ClientRetentionPolicy>
{
    /// <summary>
    /// Maps per-client retention policies used by governance operations.
    /// </summary>
    public void Configure(EntityTypeBuilder<ClientRetentionPolicy> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.RetentionDays).IsRequired();
        builder.HasIndex(item => item.ClientId).IsUnique();
        builder.HasOne(item => item.Client)
            .WithOne(client => client.RetentionPolicy)
            .HasForeignKey<ClientRetentionPolicy>(item => item.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
