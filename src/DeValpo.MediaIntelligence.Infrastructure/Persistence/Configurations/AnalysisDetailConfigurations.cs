using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Configurations;

public sealed class SkillExecutionConfiguration : IEntityTypeConfiguration<SkillExecution>
{
    /// <summary>
    /// Maps skill execution audit rows and their relationship to analysis jobs.
    /// </summary>
    public void Configure(EntityTypeBuilder<SkillExecution> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.SkillName).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Status).HasMaxLength(50).IsRequired();
        builder.Property(item => item.Confidence).HasPrecision(8, 4);
        builder.Property(item => item.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(item => item.JobId);
        builder.HasOne(item => item.Job).WithMany(job => job.SkillExecutions).HasForeignKey(item => item.JobId);
    }
}

public sealed class ExtractedFieldConfiguration : IEntityTypeConfiguration<ExtractedField>
{
    /// <summary>
    /// Maps extracted fields produced by analysis skills.
    /// </summary>
    public void Configure(EntityTypeBuilder<ExtractedField> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Name).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Value).HasColumnType("text");
        builder.Property(item => item.Confidence).HasPrecision(8, 4);
        builder.Property(item => item.SourceSkill).HasMaxLength(150).IsRequired();
        builder.HasOne(item => item.AnalysisResult).WithMany(result => result.ExtractedFields).HasForeignKey(item => item.AnalysisResultId);
    }
}

public sealed class DetectedEntityConfiguration : IEntityTypeConfiguration<DetectedEntity>
{
    /// <summary>
    /// Maps named entities detected during analysis.
    /// </summary>
    public void Configure(EntityTypeBuilder<DetectedEntity> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.EntityType).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Value).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.Confidence).HasPrecision(8, 4);
        builder.Property(item => item.SourceSkill).HasMaxLength(150).IsRequired();
        builder.HasOne(item => item.AnalysisResult).WithMany(result => result.DetectedEntities).HasForeignKey(item => item.AnalysisResultId);
    }
}

public sealed class DetectedLabelConfiguration : IEntityTypeConfiguration<DetectedLabel>
{
    /// <summary>
    /// Maps labels detected during media analysis.
    /// </summary>
    public void Configure(EntityTypeBuilder<DetectedLabel> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Label).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Confidence).HasPrecision(8, 4);
        builder.Property(item => item.SourceSkill).HasMaxLength(150).IsRequired();
        builder.HasOne(item => item.AnalysisResult).WithMany(result => result.DetectedLabels).HasForeignKey(item => item.AnalysisResultId);
    }
}

public sealed class GeneratedClipConfiguration : IEntityTypeConfiguration<GeneratedClip>
{
    /// <summary>
    /// Maps generated clip metadata linked to an analysis result.
    /// </summary>
    public void Configure(EntityTypeBuilder<GeneratedClip> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ClipKey).HasMaxLength(150).IsRequired();
        builder.Property(item => item.StoragePath).HasMaxLength(500);
        builder.Property(item => item.Confidence).HasPrecision(8, 4);
        builder.Property(item => item.SourceSkill).HasMaxLength(150).IsRequired();
        builder.HasOne(item => item.AnalysisResult).WithMany(result => result.GeneratedClips).HasForeignKey(item => item.AnalysisResultId);
    }
}

public sealed class AnalysisIssueConfiguration : IEntityTypeConfiguration<AnalysisIssue>
{
    /// <summary>
    /// Maps issues raised by skills or orchestration.
    /// </summary>
    public void Configure(EntityTypeBuilder<AnalysisIssue> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Code).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Message).HasMaxLength(2000).IsRequired();
        builder.Property(item => item.Severity).HasMaxLength(50).IsRequired();
        builder.Property(item => item.SourceSkill).HasMaxLength(150).IsRequired();
        builder.HasOne(item => item.AnalysisResult).WithMany(result => result.Issues).HasForeignKey(item => item.AnalysisResultId);
    }
}

public sealed class HumanReviewTaskConfiguration : IEntityTypeConfiguration<HumanReviewTask>
{
    /// <summary>
    /// Maps review tasks opened when analysis confidence or issues require human validation.
    /// </summary>
    public void Configure(EntityTypeBuilder<HumanReviewTask> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Status).HasMaxLength(50).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(1000).IsRequired();
        builder.HasIndex(item => item.JobId);
        builder.HasOne(item => item.Job).WithMany(job => job.HumanReviewTasks).HasForeignKey(item => item.JobId);
    }
}

public sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    /// <summary>
    /// Maps webhook delivery attempts for downstream integrations.
    /// </summary>
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.TargetKey).HasMaxLength(150).IsRequired();
        builder.Property(item => item.Status).HasMaxLength(50).IsRequired();
        builder.HasIndex(item => item.JobId);
    }
}
