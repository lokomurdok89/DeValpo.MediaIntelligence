using DeValpo.MediaIntelligence.Contracts.Providers;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IGoogleVisionStatusService
{
    /// <summary>
    /// Evaluates local Google Vision configuration and optionally checks credential usability.
    /// </summary>
    Task<GoogleVisionStatusResponse> GetStatusAsync(
        bool checkRemote,
        CancellationToken cancellationToken);
}
