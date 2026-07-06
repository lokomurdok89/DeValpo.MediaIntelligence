using DeValpo.MediaIntelligence.Contracts.Skills;

namespace DeValpo.MediaIntelligence.Application.Skills;

public interface ISkillApplicationService
{
    /// <summary>
    /// Returns the registered skill catalog.
    /// </summary>
    Task<IReadOnlyCollection<SkillResponse>> ListAsync(CancellationToken cancellationToken);
}
