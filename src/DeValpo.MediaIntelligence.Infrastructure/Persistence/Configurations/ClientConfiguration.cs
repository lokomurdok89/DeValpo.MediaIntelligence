using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ClientKey).HasMaxLength(100).IsRequired();
        builder.Property(item => item.DisplayName).HasMaxLength(200).IsRequired();
        builder.HasIndex(item => item.ClientKey).IsUnique();
    }
}
