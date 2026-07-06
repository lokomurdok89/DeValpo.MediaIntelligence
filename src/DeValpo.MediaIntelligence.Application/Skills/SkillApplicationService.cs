using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Skills;

namespace DeValpo.MediaIntelligence.Application.Skills;

public interface ISkillApplicationService
{
    Task<IReadOnlyCollection<SkillResponse>> ListAsync(CancellationToken cancellationToken);
}

public sealed class SkillApplicationService(IEnumerable<IMediaAnalysisSkill> skills) : ISkillApplicationService
{
    public Task<IReadOnlyCollection<SkillResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var duplicateName = skills
            .GroupBy(skill => skill.SkillName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;

        if (duplicateName is not null)
        {
            throw new InvalidOperationException($"Duplicate skill name registered: {duplicateName}.");
        }

        var response = skills
            .OrderBy(skill => skill.SkillName)
            .Select(skill => new SkillResponse(
                skill.SkillName,
                skill.SupportedAssetTypes,
                skill.RequiredCapabilities,
                skill.Description))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<SkillResponse>>(response);
    }
}
