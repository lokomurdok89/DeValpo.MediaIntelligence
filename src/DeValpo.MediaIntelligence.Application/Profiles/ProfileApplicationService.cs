using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Profiles;

namespace DeValpo.MediaIntelligence.Application.Profiles;

public sealed class ProfileApplicationService(IAnalysisProfileResolver profileResolver) : IProfileApplicationService
{
    /// <summary>
    /// Maps internal profile definitions to their contract response shape.
    /// </summary>
    public async Task<IReadOnlyCollection<AnalysisProfileResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var profiles = await profileResolver.ListAsync(cancellationToken);

        return profiles
            .OrderBy(profile => profile.ProfileKey)
            .Select(profile => new AnalysisProfileResponse(
                profile.ProfileKey,
                profile.ClientKey,
                profile.SupportedAssetTypes,
                profile.SkillNames,
                profile.MinimumConfidence,
                profile.HumanReviewEnabled,
                profile.Description))
            .ToArray();
    }
}
