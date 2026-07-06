using DeValpo.MediaIntelligence.Contracts;

namespace DeValpo.MediaIntelligence.Application.Models;

public sealed record AnalysisProfileDefinition(
    string ProfileKey,
    string ClientKey,
    IReadOnlyCollection<MediaAssetType> SupportedAssetTypes,
    IReadOnlyCollection<string> SkillNames,
    decimal MinimumConfidence,
    bool HumanReviewEnabled,
    string Description);
