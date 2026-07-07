using DeValpo.MediaIntelligence.Contracts.Providers;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IGoogleVideoIntelligenceStatusService
{
    /// <summary>
    /// Evaluates local Google Video Intelligence configuration and optionally checks the staging bucket.
    /// </summary>
    Task<GoogleVideoIntelligenceStatusResponse> GetStatusAsync(
        bool checkRemote,
        CancellationToken cancellationToken);
}
