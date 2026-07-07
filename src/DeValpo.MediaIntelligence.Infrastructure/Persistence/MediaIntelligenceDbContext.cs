using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence;

public sealed class MediaIntelligenceDbContext(DbContextOptions<MediaIntelligenceDbContext> options)
    : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientApiKey> ClientApiKeys => Set<ClientApiKey>();
    public DbSet<ClientRetentionPolicy> ClientRetentionPolicies => Set<ClientRetentionPolicy>();
    public DbSet<ClientWebhookTarget> ClientWebhookTargets => Set<ClientWebhookTarget>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<AnalysisProfile> AnalysisProfiles => Set<AnalysisProfile>();
    public DbSet<AnalysisJob> AnalysisJobs => Set<AnalysisJob>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<SkillExecution> SkillExecutions => Set<SkillExecution>();
    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();
    public DbSet<ExtractedField> ExtractedFields => Set<ExtractedField>();
    public DbSet<DetectedEntity> DetectedEntities => Set<DetectedEntity>();
    public DbSet<DetectedLabel> DetectedLabels => Set<DetectedLabel>();
    public DbSet<GeneratedClip> GeneratedClips => Set<GeneratedClip>();
    public DbSet<AnalysisIssue> AnalysisIssues => Set<AnalysisIssue>();
    public DbSet<HumanReviewTask> HumanReviewTasks => Set<HumanReviewTask>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    /// <summary>
    /// Applies all entity mappings declared in the infrastructure assembly.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MediaIntelligenceDbContext).Assembly);
    }
}
