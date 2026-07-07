using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using Google.Apis.Auth.OAuth2;
using System.Text.Json;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Google.VideoIntelligence;

internal static class GoogleVideoIntelligenceCredentialProvider
{
    private const string CloudPlatformScope = "https://www.googleapis.com/auth/cloud-platform";

    /// <summary>
    /// Resolves Google credentials for Video Intelligence without exposing credential contents.
    /// </summary>
    public static async Task<GoogleCredential> GetCredentialAsync(
        GoogleVideoIntelligenceOptions options,
        CancellationToken cancellationToken)
    {
        var credential = string.IsNullOrWhiteSpace(options.CredentialsPath)
            ? await GoogleCredential.GetApplicationDefaultAsync(cancellationToken)
            : await GetCredentialFromFileAsync(options, cancellationToken);

        return credential.IsCreateScopedRequired
            ? credential.CreateScoped(CloudPlatformScope)
            : credential;
    }

    private static async Task<GoogleCredential> GetCredentialFromFileAsync(
        GoogleVideoIntelligenceOptions options,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(options.CredentialsPath))
        {
            throw new FileNotFoundException(InfrastructureResourceText.GoogleVideoIntelligenceCredentialsFileNotFound);
        }

        await using var stream = File.OpenRead(options.CredentialsPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var credentialType = document.RootElement.TryGetProperty("type", out var typeElement)
            ? typeElement.GetString() ?? string.Empty
            : string.Empty;

        if (string.Equals(credentialType, JsonCredentialParameters.AuthorizedUserCredentialType, StringComparison.Ordinal) &&
            !options.AllowUserCredentials)
        {
            throw new InvalidOperationException(InfrastructureResourceText.GoogleVideoIntelligenceUserCredentialsNotAllowed);
        }

        if (!string.Equals(credentialType, JsonCredentialParameters.ServiceAccountCredentialType, StringComparison.Ordinal) &&
            !string.Equals(credentialType, JsonCredentialParameters.AuthorizedUserCredentialType, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(InfrastructureResourceText.GoogleVideoIntelligenceCredentialsInvalid);
        }

        return await CredentialFactory.FromFileAsync(
            options.CredentialsPath,
            credentialType,
            cancellationToken);
    }
}
