using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Configurations;

public sealed class AnalysisProfileConfiguration : IEntityTypeConfiguration<AnalysisProfile>
{
    public void Configure(EntityTypeBuilder<AnalysisProfile> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ProfileKey).HasMaxLength(150).IsRequired();
        builder.Property(item => item.ClientKey).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(500).IsRequired();
        builder.Property(item => item.DefinitionJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(item => item.ProfileKey).IsUnique();
    }
}
