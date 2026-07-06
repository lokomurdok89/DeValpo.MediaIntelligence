using DeValpo.MediaIntelligence.Contracts;

namespace DeValpo.MediaIntelligence.Application.Models;

public sealed record AnalysisOrchestrationRequest(
    Guid JobId,
    string ClientKey,
    string ProfileKey,
    MediaAssetType AssetType,
    IReadOnlyDictionary<string, string> Metadata);
