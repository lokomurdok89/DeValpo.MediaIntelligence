using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Profiles;

namespace DeValpo.MediaIntelligence.Application.Profiles;

public interface IProfileApplicationService
{
    Task<IReadOnlyCollection<AnalysisProfileResponse>> ListAsync(CancellationToken cancellationToken);
}

public sealed class ProfileApplicationService(IAnalysisProfileResolver profileResolver) : IProfileApplicationService
{
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
