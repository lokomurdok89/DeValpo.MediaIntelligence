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
