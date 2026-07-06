using DeValpo.MediaIntelligence.Contracts.Providers;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IGoogleDocumentAiStatusService
{
    /// <summary>
    /// Checks local Google Document AI configuration and optionally validates the remote processor.
    /// </summary>
    Task<GoogleDocumentAiStatusResponse> GetStatusAsync(bool checkRemote, CancellationToken cancellationToken);
}
