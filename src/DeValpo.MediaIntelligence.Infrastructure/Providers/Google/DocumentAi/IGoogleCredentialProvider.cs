using DeValpo.MediaIntelligence.Infrastructure.Options;
using Google.Apis.Auth.OAuth2;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Google.DocumentAi;

public interface IGoogleCredentialProvider
{
    /// <summary>
    /// Resolves scoped Google credentials from a mounted file or Application Default Credentials.
    /// </summary>
    Task<GoogleCredential> GetCredentialAsync(
        GoogleDocumentAiOptions options,
        CancellationToken cancellationToken);
}
