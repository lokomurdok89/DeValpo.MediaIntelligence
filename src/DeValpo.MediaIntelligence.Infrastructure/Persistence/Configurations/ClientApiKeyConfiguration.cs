using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Configurations;

public sealed class ClientApiKeyConfiguration : IEntityTypeConfiguration<ClientApiKey>
{
    /// <summary>
    /// Maps API key hashes and key metadata without storing raw secrets.
    /// </summary>
    public void Configure(EntityTypeBuilder<ClientApiKey> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Name).HasMaxLength(150).IsRequired();
        builder.Property(item => item.KeyPrefix).HasMaxLength(32).IsRequired();
        builder.Property(item => item.KeyHash).HasMaxLength(128).IsRequired();
        builder.Property(item => item.Scope).HasMaxLength(500).IsRequired();

        builder.HasIndex(item => item.KeyHash).IsUnique();
        builder.HasIndex(item => new { item.ClientId, item.KeyPrefix });

        builder.HasOne(item => item.Client)
            .WithMany(client => client.ApiKeys)
            .HasForeignKey(item => item.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
