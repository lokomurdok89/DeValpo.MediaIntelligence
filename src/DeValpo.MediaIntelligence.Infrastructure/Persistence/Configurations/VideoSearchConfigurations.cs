using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Configurations;

public sealed class VideoSearchJobConfiguration : IEntityTypeConfiguration<VideoSearchJob>
{
    /// <summary>
    /// Maps video search job metadata and queue state.
    /// </summary>
    public void Configure(EntityTypeBuilder<VideoSearchJob> builder)
    {
        builder.Property(item => item.ClientKey).HasMaxLength(128).IsRequired();
        builder.Property(item => item.ProfileKey).HasMaxLength(128).IsRequired();
        builder.Property(item => item.Status).HasMaxLength(32).IsRequired();
        builder.Property(item => item.ManifestJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.ConsentReferenceId).HasMaxLength(256);
        builder.Property(item => item.ErrorMessage).HasMaxLength(2048);
        builder.HasIndex(item => new { item.Status, item.CreatedAt });
        builder.HasMany(item => item.Assets).WithOne(item => item.Job).HasForeignKey(item => item.JobId);
        builder.HasMany(item => item.TargetMatchSegments).WithOne(item => item.Job).HasForeignKey(item => item.JobId);
        builder.HasMany(item => item.FaceOccurrenceSegments).WithOne(item => item.Job).HasForeignKey(item => item.JobId);
        builder.HasMany(item => item.FaceReferenceMatchSegments).WithOne(item => item.Job).HasForeignKey(item => item.JobId);
        builder.HasMany(item => item.Issues).WithOne(item => item.Job).HasForeignKey(item => item.JobId);
    }
}

public sealed class VideoSearchAssetConfiguration : IEntityTypeConfiguration<VideoSearchAsset>
{
    /// <summary>
    /// Maps uploaded source videos and their provider staging metadata.
    /// </summary>
    public void Configure(EntityTypeBuilder<VideoSearchAsset> builder)
    {
        builder.Property(item => item.AssetKey).HasMaxLength(128).IsRequired();
        builder.Property(item => item.AssetRole).HasMaxLength(64).IsRequired();
        builder.Property(item => item.OriginalFileName).HasMaxLength(512).IsRequired();
        builder.Property(item => item.StoragePath).HasMaxLength(1024).IsRequired();
        builder.Property(item => item.ContentType).HasMaxLength(256).IsRequired();
        builder.Property(item => item.ProviderInputUri).HasMaxLength(2048);
        builder.Property(item => item.VideoDurationSeconds).HasPrecision(18, 3);
        builder.HasIndex(item => new { item.JobId, item.AssetKey }).IsUnique();
        builder.HasIndex(item => item.DeletedAt);
    }
}

public sealed class VideoSearchTargetMatchSegmentConfiguration : IEntityTypeConfiguration<VideoSearchTargetMatchSegment>
{
    /// <summary>
    /// Maps normalized target matches produced from visible numbers in video OCR.
    /// </summary>
    public void Configure(EntityTypeBuilder<VideoSearchTargetMatchSegment> builder)
    {
        builder.Property(item => item.TargetKey).HasMaxLength(128).IsRequired();
        builder.Property(item => item.VisibleNumber).HasMaxLength(64).IsRequired();
        builder.Property(item => item.VideoKey).HasMaxLength(128).IsRequired();
        builder.Property(item => item.VideoFileName).HasMaxLength(512).IsRequired();
        builder.Property(item => item.StartTimeSeconds).HasPrecision(18, 3);
        builder.Property(item => item.EndTimeSeconds).HasPrecision(18, 3);
        builder.Property(item => item.Confidence).HasPrecision(5, 4);
        builder.Property(item => item.SourceProvider).HasMaxLength(128).IsRequired();
        builder.HasOne(item => item.VideoAsset).WithMany().HasForeignKey(item => item.VideoAssetId);
        builder.HasIndex(item => new { item.JobId, item.TargetKey, item.VideoKey });
    }
}

public sealed class VideoSearchFaceOccurrenceSegmentConfiguration : IEntityTypeConfiguration<VideoSearchFaceOccurrenceSegment>
{
    /// <summary>
    /// Maps generic face occurrence segments that are not linked to identity or target matching.
    /// </summary>
    public void Configure(EntityTypeBuilder<VideoSearchFaceOccurrenceSegment> builder)
    {
        builder.Property(item => item.VideoKey).HasMaxLength(128).IsRequired();
        builder.Property(item => item.VideoFileName).HasMaxLength(512).IsRequired();
        builder.Property(item => item.StartTimeSeconds).HasPrecision(18, 3);
        builder.Property(item => item.EndTimeSeconds).HasPrecision(18, 3);
        builder.Property(item => item.Confidence).HasPrecision(5, 4);
        builder.Property(item => item.SourceProvider).HasMaxLength(128).IsRequired();
        builder.HasOne(item => item.VideoAsset).WithMany().HasForeignKey(item => item.VideoAssetId);
        builder.HasIndex(item => new { item.JobId, item.VideoKey });
    }
}

public sealed class VideoSearchFaceReferenceMatchSegmentConfiguration : IEntityTypeConfiguration<VideoSearchFaceReferenceMatchSegment>
{
    /// <summary>
    /// Maps AWS Rekognition reference-face matches returned from consented face search jobs.
    /// </summary>
    public void Configure(EntityTypeBuilder<VideoSearchFaceReferenceMatchSegment> builder)
    {
        builder.Property(item => item.TargetKey).HasMaxLength(128).IsRequired();
        builder.Property(item => item.ReferenceImageKey).HasMaxLength(128).IsRequired();
        builder.Property(item => item.VideoKey).HasMaxLength(128).IsRequired();
        builder.Property(item => item.VideoFileName).HasMaxLength(512).IsRequired();
        builder.Property(item => item.StartTimeSeconds).HasPrecision(18, 3);
        builder.Property(item => item.EndTimeSeconds).HasPrecision(18, 3);
        builder.Property(item => item.Similarity).HasPrecision(5, 2);
        builder.Property(item => item.MatchType).HasMaxLength(64).IsRequired();
        builder.Property(item => item.SourceProvider).HasMaxLength(128).IsRequired();
        builder.HasOne(item => item.VideoAsset).WithMany().HasForeignKey(item => item.VideoAssetId);
        builder.HasIndex(item => new { item.JobId, item.TargetKey, item.VideoKey });
    }
}

public sealed class VideoSearchIssueConfiguration : IEntityTypeConfiguration<VideoSearchIssue>
{
    /// <summary>
    /// Maps provider and validation issues produced during video search processing.
    /// </summary>
    public void Configure(EntityTypeBuilder<VideoSearchIssue> builder)
    {
        builder.Property(item => item.Code).HasMaxLength(128).IsRequired();
        builder.Property(item => item.Message).HasMaxLength(2048).IsRequired();
        builder.Property(item => item.Severity).HasMaxLength(32).IsRequired();
        builder.Property(item => item.SourceProvider).HasMaxLength(128).IsRequired();
    }
}
