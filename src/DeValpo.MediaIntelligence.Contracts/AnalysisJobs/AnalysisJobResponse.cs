using DeValpo.MediaIntelligence.Contracts;

namespace DeValpo.MediaIntelligence.Contracts.AnalysisJobs;

public sealed record AnalysisJobResponse(
    Guid JobId,
    string ClientKey,
    string ProfileKey,
    MediaAssetType AssetType,
    AnalysisJobStatus Status,
    int ProcessingAttempts,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? ErrorMessage);
