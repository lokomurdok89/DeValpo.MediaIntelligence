using System.Text.Json;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using Google.Apis.Auth.OAuth2;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Google.Vision;

internal static class GoogleVisionCredentialProvider
{
    private const string CloudPlatformScope = "https://www.googleapis.com/auth/cloud-platform";

    /// <summary>
    /// Resolves Google Vision credentials without exposing credential file content.
    /// </summary>
    public static async Task<GoogleCredential> GetCredentialAsync(
        GoogleVisionOptions options,
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
        GoogleVisionOptions options,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(options.CredentialsPath))
        {
            throw new FileNotFoundException(InfrastructureResourceText.GoogleVisionCredentialsFileNotFound);
        }

        await using var stream = File.OpenRead(options.CredentialsPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var credentialType = document.RootElement.TryGetProperty("type", out var typeElement)
            ? typeElement.GetString() ?? string.Empty
            : string.Empty;

        if (string.Equals(credentialType, JsonCredentialParameters.AuthorizedUserCredentialType, StringComparison.Ordinal) &&
            !options.AllowUserCredentials)
        {
            throw new InvalidOperationException(InfrastructureResourceText.GoogleVisionUserCredentialsNotAllowed);
        }

        if (!string.Equals(credentialType, JsonCredentialParameters.ServiceAccountCredentialType, StringComparison.Ordinal) &&
            !string.Equals(credentialType, JsonCredentialParameters.AuthorizedUserCredentialType, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(InfrastructureResourceText.GoogleVisionCredentialsInvalid);
        }

        return await CredentialFactory.FromFileAsync(
            options.CredentialsPath,
            credentialType,
            cancellationToken);
    }
}
