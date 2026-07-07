using System.Globalization;
using System.Reflection;
using System.Resources;

namespace DeValpo.MediaIntelligence.Api.Resources;

internal static class ApiResourceText
{
    private static readonly ResourceManager Resources = new(
        "DeValpo.MediaIntelligence.Api.Resources.ApiMessages",
        Assembly.GetExecutingAssembly());

    public static string FileRequired => GetRequiredResource(nameof(FileRequired));
    public static string FileEmpty => GetRequiredResource(nameof(FileEmpty));
    public static string FileExceedsMaxSize(long maxFileSizeBytes) => Format(nameof(FileExceedsMaxSize), maxFileSizeBytes);
    public static string FileExtensionUnsupported(string extension) => Format(nameof(FileExtensionUnsupported), extension);
    public static string FileExtensionNotSupportedForAssetType(string extension, object assetType) => Format(nameof(FileExtensionNotSupportedForAssetType), extension, assetType);
    public static string FileContentTypeUnsupported(string contentType) => Format(nameof(FileContentTypeUnsupported), contentType);
    public static string FileSignatureInvalid => GetRequiredResource(nameof(FileSignatureInvalid));
    public static string PdfCorrupt => GetRequiredResource(nameof(PdfCorrupt));
    public static string FileThreatDetected => GetRequiredResource(nameof(FileThreatDetected));
    public static string MetadataJsonMustBeObject => GetRequiredResource(nameof(MetadataJsonMustBeObject));
    public static string MetadataJsonOnlyStringValues => GetRequiredResource(nameof(MetadataJsonOnlyStringValues));
    public static string MetadataJsonMustBeValidJson => GetRequiredResource(nameof(MetadataJsonMustBeValidJson));
    public static string HealthStatusHealthy => GetRequiredResource(nameof(HealthStatusHealthy));
    public static string LocalStorageHealthy => GetRequiredResource(nameof(LocalStorageHealthy));
    public static string LocalStorageUnhealthy => GetRequiredResource(nameof(LocalStorageUnhealthy));
    public static string PostgresHealthy => GetRequiredResource(nameof(PostgresHealthy));
    public static string PostgresUnhealthy => GetRequiredResource(nameof(PostgresUnhealthy));
    public static string GoogleDocumentAiReadinessDisabled => GetRequiredResource(nameof(GoogleDocumentAiReadinessDisabled));
    public static string GoogleDocumentAiReadinessHealthy => GetRequiredResource(nameof(GoogleDocumentAiReadinessHealthy));
    public static string GoogleDocumentAiReadinessUnhealthy => GetRequiredResource(nameof(GoogleDocumentAiReadinessUnhealthy));
    public static string ConnectionStringRequired => GetRequiredResource(nameof(ConnectionStringRequired));
    public static string RequestFailedLogMessage => GetRequiredResource(nameof(RequestFailedLogMessage));
    public static string UnexpectedError => GetRequiredResource(nameof(UnexpectedError));
    public static string JwtIssuerRequired => GetRequiredResource(nameof(JwtIssuerRequired));
    public static string JwtAudienceRequired => GetRequiredResource(nameof(JwtAudienceRequired));
    public static string JwtSigningKeyMinimumLength => GetRequiredResource(nameof(JwtSigningKeyMinimumLength));
    public static string JwtAccessTokenMinutesRequired => GetRequiredResource(nameof(JwtAccessTokenMinutesRequired));
    public static string ReleaseJwtSigningKeyUnsafe => GetRequiredResource(nameof(ReleaseJwtSigningKeyUnsafe));
    public static string ReleaseJwtRequireHttpsMetadataRequired => GetRequiredResource(nameof(ReleaseJwtRequireHttpsMetadataRequired));
    public static string ReleaseSwaggerMustBeDisabled => GetRequiredResource(nameof(ReleaseSwaggerMustBeDisabled));
    public static string ReleaseAutomaticMigrationsDisabled => GetRequiredResource(nameof(ReleaseAutomaticMigrationsDisabled));
    public static string ReleaseGoogleUserCredentialsDisabled => GetRequiredResource(nameof(ReleaseGoogleUserCredentialsDisabled));
    public static string ReleaseGoogleVideoUserCredentialsDisabled => GetRequiredResource(nameof(ReleaseGoogleVideoUserCredentialsDisabled));
    public static string ReleaseMockSkillsDisabled => GetRequiredResource(nameof(ReleaseMockSkillsDisabled));
    public static string ReleaseMockProfilesDisabled => GetRequiredResource(nameof(ReleaseMockProfilesDisabled));
    public static string InvalidClientCredentials => GetRequiredResource(nameof(InvalidClientCredentials));
    public static string AnalysisJobWorkerFailed => GetRequiredResource(nameof(AnalysisJobWorkerFailed));
    public static string VideoSearchJobWorkerFailed => GetRequiredResource(nameof(VideoSearchJobWorkerFailed));
    public static string WebhookDeliveryWorkerFailed => GetRequiredResource(nameof(WebhookDeliveryWorkerFailed));
    public static string VideoFilesRequired => GetRequiredResource(nameof(VideoFilesRequired));
    public static string VideoMaxFilesExceeded(int maxFiles) => Format(nameof(VideoMaxFilesExceeded), maxFiles);
    public static string SwaggerBearerDescription => GetRequiredResource(nameof(SwaggerBearerDescription));
    public static string SwaggerResourceMissing(string name) => Format(nameof(SwaggerResourceMissing), name);

    private static string GetRequiredResource(string name)
    {
        return Resources.GetString(name)
            ?? throw new InvalidOperationException($"Missing API resource '{name}'.");
    }

    private static string Format(string name, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, GetRequiredResource(name), args);
    }
}
