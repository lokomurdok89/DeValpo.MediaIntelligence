namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class SkillExecution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public AnalysisJob? Job { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
    public decimal Confidence { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ErrorMessage { get; set; }
}

public sealed class ExtractedField
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnalysisResultId { get; set; }
    public AnalysisResult? AnalysisResult { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public decimal Confidence { get; set; }
    public string SourceSkill { get; set; } = string.Empty;
}

public sealed class DetectedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnalysisResultId { get; set; }
    public AnalysisResult? AnalysisResult { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string SourceSkill { get; set; } = string.Empty;
}

public sealed class DetectedLabel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnalysisResultId { get; set; }
    public AnalysisResult? AnalysisResult { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string SourceSkill { get; set; } = string.Empty;
}

public sealed class GeneratedClip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnalysisResultId { get; set; }
    public AnalysisResult? AnalysisResult { get; set; }
    public string ClipKey { get; set; } = string.Empty;
    public string? StoragePath { get; set; }
    public decimal Confidence { get; set; }
    public string SourceSkill { get; set; } = string.Empty;
}

public sealed class AnalysisIssue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnalysisResultId { get; set; }
    public AnalysisResult? AnalysisResult { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string SourceSkill { get; set; } = string.Empty;
}

public sealed class HumanReviewTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public AnalysisJob? Job { get; set; }
    public string Status { get; set; } = "Open";
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class WebhookDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public string TargetKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastAttemptAt { get; set; }
}
