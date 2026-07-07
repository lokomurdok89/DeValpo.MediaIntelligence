using System.Globalization;
using System.Reflection;
using System.Resources;

namespace DeValpo.MediaIntelligence.Infrastructure.Resources;

internal static class InfrastructureResourceText
{
    private static readonly ResourceManager Resources = new(
        "DeValpo.MediaIntelligence.Infrastructure.Resources.InfrastructureMessages",
        Assembly.GetExecutingAssembly());

    public static string ConnectionStringRequired => GetRequiredResource(nameof(ConnectionStringRequired));
    public static string StorageProviderRequired => GetRequiredResource(nameof(StorageProviderRequired));
    public static string StorageLocalRootPathRequired => GetRequiredResource(nameof(StorageLocalRootPathRequired));
    public static string GoogleDocumentAiRequiredOptions => GetRequiredResource(nameof(GoogleDocumentAiRequiredOptions));
    public static string DuplicateSkillName(string skillName) => Format(nameof(DuplicateSkillName), skillName);
    public static string ProfileReferencesUnregisteredSkill(string profileKey, string skillName) => Format(nameof(ProfileReferencesUnregisteredSkill), profileKey, skillName);
    public static string ProductionProfileReferencesMockSkill(string profileKey, string skillName) => Format(nameof(ProductionProfileReferencesMockSkill), profileKey, skillName);
    public static string ResolvedStoragePathOutsideRoot => GetRequiredResource(nameof(ResolvedStoragePathOutsideRoot));
    public static string RequestedStoragePathOutsideRoot => GetRequiredResource(nameof(RequestedStoragePathOutsideRoot));
    public static string GoogleDocumentAiDisabled => GetRequiredResource(nameof(GoogleDocumentAiDisabled));
    public static string GoogleDocumentAiDisabledByConfiguration => GetRequiredResource(nameof(GoogleDocumentAiDisabledByConfiguration));
    public static string MissingGoogleDocumentAiConfiguration(string fields) => Format(nameof(MissingGoogleDocumentAiConfiguration), fields);
    public static string MissingGoogleDocumentAiOptions(string fields) => Format(nameof(MissingGoogleDocumentAiOptions), fields);
    public static string GoogleDocumentAiCredentialsFileNotFound => GetRequiredResource(nameof(GoogleDocumentAiCredentialsFileNotFound));
    public static string GoogleDocumentAiCredentialsCannotBeRead => GetRequiredResource(nameof(GoogleDocumentAiCredentialsCannotBeRead));
    public static string GoogleDocumentAiCredentialsInvalid => GetRequiredResource(nameof(GoogleDocumentAiCredentialsInvalid));
    public static string GoogleDocumentAiUserCredentialsNotAllowed => GetRequiredResource(nameof(GoogleDocumentAiUserCredentialsNotAllowed));
    public static string GoogleDocumentAiCredentialsInvalidOrExpired => GetRequiredResource(nameof(GoogleDocumentAiCredentialsInvalidOrExpired));
    public static string GoogleDocumentAiPermissionDenied => GetRequiredResource(nameof(GoogleDocumentAiPermissionDenied));
    public static string GoogleDocumentAiProcessorNotFound => GetRequiredResource(nameof(GoogleDocumentAiProcessorNotFound));
    public static string GoogleDocumentAiApiOrBillingUnavailable => GetRequiredResource(nameof(GoogleDocumentAiApiOrBillingUnavailable));
    public static string GoogleDocumentAiQuotaReached => GetRequiredResource(nameof(GoogleDocumentAiQuotaReached));
    public static string GoogleDocumentAiServiceUnavailable => GetRequiredResource(nameof(GoogleDocumentAiServiceUnavailable));
    public static string GoogleDocumentAiRemoteCheckFailedWithStatus(object statusCode) => Format(nameof(GoogleDocumentAiRemoteCheckFailedWithStatus), statusCode);
    public static string GoogleDocumentAiRemoteCheckTimedOut => GetRequiredResource(nameof(GoogleDocumentAiRemoteCheckTimedOut));
    public static string GoogleDocumentAiRemoteCheckFailed => GetRequiredResource(nameof(GoogleDocumentAiRemoteCheckFailed));
    public static string GoogleDocumentAiRequestFailedWithStatus(object statusCode) => Format(nameof(GoogleDocumentAiRequestFailedWithStatus), statusCode);
    public static string GoogleDocumentAiRequestFailed => GetRequiredResource(nameof(GoogleDocumentAiRequestFailed));
    public static string GoogleDocumentAiLocalConfigCompleteWithAdc => GetRequiredResource(nameof(GoogleDocumentAiLocalConfigCompleteWithAdc));
    public static string GoogleDocumentAiLocalConfigComplete => GetRequiredResource(nameof(GoogleDocumentAiLocalConfigComplete));
    public static string GoogleDocumentAiProcessorReachable => GetRequiredResource(nameof(GoogleDocumentAiProcessorReachable));
    public static string GoogleDocumentAiSkillDescription => GetRequiredResource(nameof(GoogleDocumentAiSkillDescription));
    public static string MockDocumentOcrDescription => GetRequiredResource(nameof(MockDocumentOcrDescription));
    public static string MockExtractedText => GetRequiredResource(nameof(MockExtractedText));
    public static string MockHandwrittenIdDescription => GetRequiredResource(nameof(MockHandwrittenIdDescription));
    public static string MockStampDetectionDescription => GetRequiredResource(nameof(MockStampDetectionDescription));
    public static string MockImageObjectDetectionDescription => GetRequiredResource(nameof(MockImageObjectDetectionDescription));
    public static string MockSportsBibDetectionDescription => GetRequiredResource(nameof(MockSportsBibDetectionDescription));
    public static string MockFaceDetectionDescription => GetRequiredResource(nameof(MockFaceDetectionDescription));
    public static string FaceDetectionDisabled => GetRequiredResource(nameof(FaceDetectionDisabled));
    public static string MockVideoClipGenerationDescription => GetRequiredResource(nameof(MockVideoClipGenerationDescription));
    public static string MockLiveVideoBufferDescription => GetRequiredResource(nameof(MockLiveVideoBufferDescription));
    public static string LiveVideoStructuralOnly => GetRequiredResource(nameof(LiveVideoStructuralOnly));
    public static string GoogleVideoIntelligenceDisabled => GetRequiredResource(nameof(GoogleVideoIntelligenceDisabled));
    public static string GoogleVideoIntelligenceRequiredOptions => GetRequiredResource(nameof(GoogleVideoIntelligenceRequiredOptions));
    public static string MissingGoogleVideoIntelligenceConfiguration(string fields) => Format(nameof(MissingGoogleVideoIntelligenceConfiguration), fields);
    public static string GoogleVideoIntelligenceCredentialsFileNotFound => GetRequiredResource(nameof(GoogleVideoIntelligenceCredentialsFileNotFound));
    public static string GoogleVideoIntelligenceCredentialsCannotBeRead => GetRequiredResource(nameof(GoogleVideoIntelligenceCredentialsCannotBeRead));
    public static string GoogleVideoIntelligenceCredentialsInvalid => GetRequiredResource(nameof(GoogleVideoIntelligenceCredentialsInvalid));
    public static string GoogleVideoIntelligenceUserCredentialsNotAllowed => GetRequiredResource(nameof(GoogleVideoIntelligenceUserCredentialsNotAllowed));
    public static string GoogleVideoIntelligenceCredentialsInvalidOrExpired => GetRequiredResource(nameof(GoogleVideoIntelligenceCredentialsInvalidOrExpired));
    public static string GoogleVideoIntelligencePermissionDenied => GetRequiredResource(nameof(GoogleVideoIntelligencePermissionDenied));
    public static string GoogleVideoIntelligenceBucketNotFound => GetRequiredResource(nameof(GoogleVideoIntelligenceBucketNotFound));
    public static string GoogleVideoIntelligenceApiOrBillingUnavailable => GetRequiredResource(nameof(GoogleVideoIntelligenceApiOrBillingUnavailable));
    public static string GoogleVideoIntelligenceQuotaReached => GetRequiredResource(nameof(GoogleVideoIntelligenceQuotaReached));
    public static string GoogleVideoIntelligenceServiceUnavailable => GetRequiredResource(nameof(GoogleVideoIntelligenceServiceUnavailable));
    public static string GoogleVideoIntelligenceRemoteCheckFailedWithStatus(object statusCode) => Format(nameof(GoogleVideoIntelligenceRemoteCheckFailedWithStatus), statusCode);
    public static string GoogleVideoIntelligenceRemoteCheckFailed => GetRequiredResource(nameof(GoogleVideoIntelligenceRemoteCheckFailed));
    public static string GoogleVideoIntelligenceRequestFailedWithStatus(object statusCode) => Format(nameof(GoogleVideoIntelligenceRequestFailedWithStatus), statusCode);
    public static string GoogleVideoIntelligenceRequestFailed => GetRequiredResource(nameof(GoogleVideoIntelligenceRequestFailed));
    public static string GoogleVideoIntelligenceRemoteOperationTimedOut => GetRequiredResource(nameof(GoogleVideoIntelligenceRemoteOperationTimedOut));
    public static string GoogleVideoIntelligenceRemoteOperationTimeoutRequired => GetRequiredResource(nameof(GoogleVideoIntelligenceRemoteOperationTimeoutRequired));
    public static string GoogleVideoIntelligenceLocalConfigComplete => GetRequiredResource(nameof(GoogleVideoIntelligenceLocalConfigComplete));
    public static string GoogleVideoIntelligenceBucketReachable => GetRequiredResource(nameof(GoogleVideoIntelligenceBucketReachable));
    public static string AwsRekognitionDisabled => GetRequiredResource(nameof(AwsRekognitionDisabled));
    public static string AwsRekognitionFaceSearchDisabled => GetRequiredResource(nameof(AwsRekognitionFaceSearchDisabled));
    public static string MissingAwsRekognitionConfiguration(string fields) => Format(nameof(MissingAwsRekognitionConfiguration), fields);
    public static string AwsRekognitionLocalConfigComplete => GetRequiredResource(nameof(AwsRekognitionLocalConfigComplete));
    public static string AwsRekognitionRemoteResourcesReachable => GetRequiredResource(nameof(AwsRekognitionRemoteResourcesReachable));
    public static string AwsRekognitionCredentialsUnavailable => GetRequiredResource(nameof(AwsRekognitionCredentialsUnavailable));
    public static string AwsRekognitionCredentialsFileNotFound => GetRequiredResource(nameof(AwsRekognitionCredentialsFileNotFound));
    public static string AwsRekognitionCredentialsInvalid => GetRequiredResource(nameof(AwsRekognitionCredentialsInvalid));
    public static string AwsRekognitionPermissionDenied => GetRequiredResource(nameof(AwsRekognitionPermissionDenied));
    public static string AwsRekognitionBucketNotFound => GetRequiredResource(nameof(AwsRekognitionBucketNotFound));
    public static string AwsRekognitionCollectionNotFound => GetRequiredResource(nameof(AwsRekognitionCollectionNotFound));
    public static string AwsRekognitionRemoteCheckFailedWithStatus(object statusCode) => Format(nameof(AwsRekognitionRemoteCheckFailedWithStatus), statusCode);
    public static string AwsRekognitionRemoteCheckFailed => GetRequiredResource(nameof(AwsRekognitionRemoteCheckFailed));
    public static string AwsRekognitionAnalysisDisabled => GetRequiredResource(nameof(AwsRekognitionAnalysisDisabled));
    public static string AwsRekognitionReferenceFaceNotIndexed(string referenceImageKey) => Format(nameof(AwsRekognitionReferenceFaceNotIndexed), referenceImageKey);
    public static string AwsRekognitionFaceSearchFailed => GetRequiredResource(nameof(AwsRekognitionFaceSearchFailed));
    public static string AwsRekognitionFaceSearchTimedOut => GetRequiredResource(nameof(AwsRekognitionFaceSearchTimedOut));
    public static string AwsRekognitionCleanupFailed => GetRequiredResource(nameof(AwsRekognitionCleanupFailed));
    public static string WebhookTargetNotLoaded => GetRequiredResource(nameof(WebhookTargetNotLoaded));
    public static string WebhookRequestTimedOut => GetRequiredResource(nameof(WebhookRequestTimedOut));
    public static string ClientNotFound(string clientKey) => Format(nameof(ClientNotFound), clientKey);

    private static string GetRequiredResource(string name)
    {
        return Resources.GetString(name)
            ?? throw new InvalidOperationException($"Missing infrastructure resource '{name}'.");
    }

    private static string Format(string name, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, GetRequiredResource(name), args);
    }
}
