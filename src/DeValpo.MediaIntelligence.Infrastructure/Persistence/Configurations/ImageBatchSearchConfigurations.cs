using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Configurations;

public sealed class ImageBatchSearchJobConfiguration : IEntityTypeConfiguration<ImageBatchSearchJob>
{
    /// <summary>
    /// Maps image batch search job metadata and queue state.
    /// </summary>
    public void Configure(EntityTypeBuilder<ImageBatchSearchJob> builder)
    {
        builder.Property(item => item.ClientKey).HasMaxLength(128).IsRequired();
        builder.Property(item => item.ProfileKey).HasMaxLength(128).IsRequired();
        builder.Property(item => item.Status).HasMaxLength(32).IsRequired();
        builder.Property(item => item.ManifestJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.ErrorMessage).HasMaxLength(2048);
        builder.HasIndex(item => new { item.Status, item.CreatedAt });
        builder.HasMany(item => item.Assets).WithOne(item => item.Job).HasForeignKey(item => item.JobId);
        builder.HasMany(item => item.Matches).WithOne(item => item.Job).HasForeignKey(item => item.JobId);
        builder.HasMany(item => item.Issues).WithOne(item => item.Job).HasForeignKey(item => item.JobId);
    }
}

public sealed class ImageBatchSearchAssetConfiguration : IEntityTypeConfiguration<ImageBatchSearchAsset>
{
    /// <summary>
    /// Maps uploaded source and reference images plus provider staging metadata.
    /// </summary>
    public void Configure(EntityTypeBuilder<ImageBatchSearchAsset> builder)
    {
        builder.Property(item => item.AssetKey).HasMaxLength(128).IsRequired();
        builder.Property(item => item.AssetRole).HasMaxLength(64).IsRequired();
        builder.Property(item => item.OriginalFileName).HasMaxLength(512).IsRequired();
        builder.Property(item => item.StoragePath).HasMaxLength(1024).IsRequired();
        builder.Property(item => item.InputMode).HasMaxLength(32).HasDefaultValue("MultipartUpload").IsRequired();
        builder.Property(item => item.StorageProvider).HasMaxLength(32);
        builder.Property(item => item.BucketName).HasMaxLength(256);
        builder.Property(item => item.ObjectKey).HasMaxLength(1024);
        builder.Property(item => item.Prefix).HasMaxLength(1024);
        builder.Property(item => item.Region).HasMaxLength(64);
        builder.Property(item => item.Location).HasMaxLength(64);
        builder.Property(item => item.Checksum).HasMaxLength(256);
        builder.Property(item => item.VersionId).HasMaxLength(256);
        builder.Property(item => item.Generation).HasMaxLength(256);
        builder.Property(item => item.StagingCleanupStatus).HasMaxLength(64);
        builder.Property(item => item.ContentType).HasMaxLength(256).IsRequired();
        builder.Property(item => item.ProviderInputUri).HasMaxLength(2048);
        builder.HasIndex(item => new { item.JobId, item.AssetKey }).IsUnique();
        builder.HasIndex(item => item.DeletedAt);
    }
}

public sealed class ImageBatchSearchMatchConfiguration : IEntityTypeConfiguration<ImageBatchSearchMatch>
{
    /// <summary>
    /// Maps provider-normalized image matches grouped by domain target keys.
    /// </summary>
    public void Configure(EntityTypeBuilder<ImageBatchSearchMatch> builder)
    {
        builder.Property(item => item.TargetKey).HasMaxLength(128).IsRequired();
        builder.Property(item => item.QueryType).HasMaxLength(64).IsRequired();
        builder.Property(item => item.ImageKey).HasMaxLength(128).IsRequired();
        builder.Property(item => item.ImageFileName).HasMaxLength(512).IsRequired();
        builder.Property(item => item.Score).HasPrecision(7, 4);
        builder.Property(item => item.DetectedText).HasMaxLength(256);
        builder.Property(item => item.SourceProvider).HasMaxLength(128).IsRequired();
        builder.HasOne(item => item.ImageAsset).WithMany().HasForeignKey(item => item.ImageAssetId);
        builder.HasIndex(item => new { item.JobId, item.TargetKey, item.ImageKey });
    }
}

public sealed class ImageBatchSearchIssueConfiguration : IEntityTypeConfiguration<ImageBatchSearchIssue>
{
    /// <summary>
    /// Maps provider and validation issues produced during image batch search processing.
    /// </summary>
    public void Configure(EntityTypeBuilder<ImageBatchSearchIssue> builder)
    {
        builder.Property(item => item.Code).HasMaxLength(128).IsRequired();
        builder.Property(item => item.Message).HasMaxLength(2048).IsRequired();
        builder.Property(item => item.Severity).HasMaxLength(32).IsRequired();
        builder.Property(item => item.SourceProvider).HasMaxLength(128).IsRequired();
        builder.Property(item => item.TargetKey).HasMaxLength(128);
        builder.Property(item => item.QueryType).HasMaxLength(64);
    }
}
