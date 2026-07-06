using DeValpo.MediaIntelligence.Contracts.Profiles;

namespace DeValpo.MediaIntelligence.Application.Profiles;

public interface IProfileApplicationService
{
    /// <summary>
    /// Returns the analysis profiles visible to API consumers.
    /// </summary>
    Task<IReadOnlyCollection<AnalysisProfileResponse>> ListAsync(CancellationToken cancellationToken);
}
