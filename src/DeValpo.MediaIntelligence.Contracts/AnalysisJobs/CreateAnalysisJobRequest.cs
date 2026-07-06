using DeValpo.MediaIntelligence.Contracts;

namespace DeValpo.MediaIntelligence.Contracts.AnalysisJobs;

public sealed record CreateAnalysisJobRequest(
    string ClientKey,
    string ProfileKey,
    MediaAssetType AssetType,
    string? OriginalFileName,
    string? ContentType,
    Dictionary<string, string>? Metadata);
