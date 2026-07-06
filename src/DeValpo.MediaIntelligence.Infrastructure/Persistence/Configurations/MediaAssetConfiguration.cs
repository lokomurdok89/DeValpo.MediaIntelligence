using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.AssetType).HasMaxLength(50).IsRequired();
        builder.Property(item => item.OriginalFileName).HasMaxLength(260).IsRequired();
        builder.Property(item => item.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(item => item.ContentType).HasMaxLength(150).IsRequired();
        builder.HasIndex(item => item.JobId);
        builder.HasOne(item => item.Job)
            .WithMany(job => job.MediaAssets)
            .HasForeignKey(item => item.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
