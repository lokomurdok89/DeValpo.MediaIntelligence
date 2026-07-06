using DeValpo.MediaIntelligence.Contracts.Providers;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IGoogleDocumentAiStatusService
{
    Task<GoogleDocumentAiStatusResponse> GetStatusAsync(bool checkRemote, CancellationToken cancellationToken);
}
