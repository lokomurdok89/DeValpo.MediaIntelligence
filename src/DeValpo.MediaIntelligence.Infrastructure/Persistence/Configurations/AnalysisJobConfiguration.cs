using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Configurations;

public sealed class AnalysisJobConfiguration : IEntityTypeConfiguration<AnalysisJob>
{
    public void Configure(EntityTypeBuilder<AnalysisJob> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ClientKey).HasMaxLength(100).IsRequired();
        builder.Property(item => item.ProfileKey).HasMaxLength(150).IsRequired();
        builder.Property(item => item.AssetType).HasMaxLength(50).IsRequired();
        builder.Property(item => item.Status).HasMaxLength(50).IsRequired();
        builder.Property(item => item.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(item => item.ClientKey);
        builder.HasIndex(item => item.ProfileKey);
        builder.HasIndex(item => item.Status);
    }
}
