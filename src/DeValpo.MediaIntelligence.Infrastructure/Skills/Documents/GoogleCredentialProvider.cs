using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using Google.Apis.Auth.OAuth2;
using System.Text.Json;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills.Documents;

internal sealed class GoogleCredentialProvider : IGoogleCredentialProvider
{
    private const string CloudPlatformScope = "https://www.googleapis.com/auth/cloud-platform";

    /// <summary>
    /// Resolves credentials without logging or exposing credential file content.
    /// </summary>
    public async Task<GoogleCredential> GetCredentialAsync(
        GoogleDocumentAiOptions options,
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
        GoogleDocumentAiOptions options,
        CancellationToken cancellationToken)
    {
        var credentialsPath = options.CredentialsPath;

        if (!File.Exists(credentialsPath))
        {
            throw new FileNotFoundException(InfrastructureResourceText.GoogleDocumentAiCredentialsFileNotFound);
        }

        var credentialType = await ReadCredentialTypeAsync(credentialsPath, cancellationToken);

        if (string.Equals(credentialType, JsonCredentialParameters.AuthorizedUserCredentialType, StringComparison.Ordinal) &&
            !options.AllowUserCredentials)
        {
            throw new InvalidOperationException(InfrastructureResourceText.GoogleDocumentAiUserCredentialsNotAllowed);
        }

        if (!IsSupportedCredentialType(credentialType))
        {
            throw new InvalidOperationException(InfrastructureResourceText.GoogleDocumentAiCredentialsInvalid);
        }

        var credential = await CredentialFactory.FromFileAsync(
            credentialsPath,
            credentialType,
            cancellationToken);

        return credential;
    }

    private static async Task<string> ReadCredentialTypeAsync(
        string credentialsPath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(credentialsPath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return document.RootElement.TryGetProperty("type", out var typeElement)
            ? typeElement.GetString() ?? string.Empty
            : string.Empty;
    }

    private static bool IsSupportedCredentialType(string credentialType)
    {
        return string.Equals(credentialType, JsonCredentialParameters.ServiceAccountCredentialType, StringComparison.Ordinal) ||
            string.Equals(credentialType, JsonCredentialParameters.AuthorizedUserCredentialType, StringComparison.Ordinal);
    }
}
