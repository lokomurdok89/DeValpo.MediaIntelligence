using DeValpo.MediaIntelligence.Contracts;

namespace DeValpo.MediaIntelligence.Application.Models;

public sealed record SkillExecutionContext(
    Guid JobId,
    string ClientKey,
    string ProfileKey,
    MediaAssetType AssetType,
    Stream Content,
    IReadOnlyDictionary<string, string> Metadata);
