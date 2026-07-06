using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Configurations;

public sealed class AnalysisResultConfiguration : IEntityTypeConfiguration<AnalysisResult>
{
    public void Configure(EntityTypeBuilder<AnalysisResult> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Confidence).HasPrecision(8, 4);
        builder.HasIndex(item => item.JobId).IsUnique();
        builder.HasOne(item => item.Job)
            .WithOne(job => job.AnalysisResult)
            .HasForeignKey<AnalysisResult>(item => item.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
