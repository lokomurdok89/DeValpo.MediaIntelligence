using DeValpo.MediaIntelligence.Contracts;

namespace DeValpo.MediaIntelligence.Contracts.Skills;

public sealed record SkillResponse(
    string SkillName,
    IReadOnlyCollection<MediaAssetType> SupportedAssetTypes,
    IReadOnlyCollection<string> RequiredCapabilities,
    string Description);
