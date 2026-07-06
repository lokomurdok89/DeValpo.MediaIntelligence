namespace DeValpo.MediaIntelligence.Contracts.AnalysisJobs;

public sealed record CreateAnalysisJobResponse(
    Guid JobId,
    string Status,
    int ProcessingAttempts,
    DateTimeOffset CreatedAt);
