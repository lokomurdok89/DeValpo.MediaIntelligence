using DeValpo.MediaIntelligence.Contracts.Capabilities;

namespace DeValpo.MediaIntelligence.Application.Capabilities;

public interface ICapabilityApplicationService
{
    /// <summary>
    /// Returns the public capability catalog exposed by the application.
    /// </summary>
    Task<IReadOnlyCollection<CapabilityResponse>> ListAsync(CancellationToken cancellationToken);
}
