using DeValpo.MediaIntelligence.Contracts;

namespace DeValpo.MediaIntelligence.Contracts.Profiles;

public sealed record AnalysisProfileResponse(
    string ProfileKey,
    string ClientKey,
    IReadOnlyCollection<MediaAssetType> SupportedAssetTypes,
    IReadOnlyCollection<string> SkillNames,
    decimal MinimumConfidence,
    bool HumanReviewEnabled,
    string Description);
