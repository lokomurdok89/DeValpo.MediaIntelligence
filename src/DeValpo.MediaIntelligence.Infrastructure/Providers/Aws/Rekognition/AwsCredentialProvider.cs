using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Resources;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition;

internal sealed class AwsCredentialProvider : IAwsCredentialProvider
{
    private const string DefaultProfileName = "default";

    /// <summary>
    /// Resolves credentials without logging or exposing key material.
    /// </summary>
    public AWSCredentials GetCredentials(AwsRekognitionOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CredentialsPath))
        {
#pragma warning disable CS0618 // AWS SDK still supports this fallback chain for local/environment credentials.
            return FallbackCredentialsFactory.GetCredentials();
#pragma warning restore CS0618
        }

        if (!File.Exists(options.CredentialsPath))
        {
            throw new FileNotFoundException(InfrastructureResourceText.AwsRekognitionCredentialsFileNotFound);
        }

        var profileName = string.IsNullOrWhiteSpace(options.ProfileName)
            ? DefaultProfileName
            : options.ProfileName.Trim();
        var credentialsFile = new SharedCredentialsFile(options.CredentialsPath);

        if (!credentialsFile.TryGetProfile(profileName, out var profile) ||
            !AWSCredentialsFactory.TryGetAWSCredentials(profile, credentialsFile, out var credentials))
        {
            throw new InvalidOperationException(InfrastructureResourceText.AwsRekognitionCredentialsInvalid);
        }

        return credentials;
    }

    public bool IsCredentialSourcePresent(AwsRekognitionOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.CredentialsPath))
        {
            return File.Exists(options.CredentialsPath);
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")) &&
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_PROFILE")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_RELATIVE_URI")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_FULL_URI")))
        {
            return true;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return !string.IsNullOrWhiteSpace(home) &&
            File.Exists(Path.Combine(home, ".aws", "credentials"));
    }

    public bool IsCredentialFilePresent(AwsRekognitionOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.CredentialsPath) && File.Exists(options.CredentialsPath);
    }
}
