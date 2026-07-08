using System.Globalization;
using System.Reflection;
using System.Resources;

namespace DeValpo.MediaIntelligence.Application.Resources;

internal static class ApplicationResourceText
{
    private static readonly ResourceManager Resources = new(
        "DeValpo.MediaIntelligence.Application.Resources.ApplicationMessages",
        Assembly.GetExecutingAssembly());

    public static string ProfileNotFound(string profileKey) => Format(nameof(ProfileNotFound), profileKey);
    public static string ProfileAssetTypeUnsupported(string profileKey, object assetType) => Format(nameof(ProfileAssetTypeUnsupported), profileKey, assetType);
    public static string JobNotFound(Guid jobId) => Format(nameof(JobNotFound), jobId);
    public static string JobCannotBeCancelled(Guid jobId, string status) => Format(nameof(JobCannotBeCancelled), jobId, status);
    public static string ClientKeyRequired => GetRequiredResource(nameof(ClientKeyRequired));
    public static string ProfileKeyRequired => GetRequiredResource(nameof(ProfileKeyRequired));
    public static string AssetTypeRequired => GetRequiredResource(nameof(AssetTypeRequired));
    public static string DuplicateSkillName(string skillName) => Format(nameof(DuplicateSkillName), skillName);
    public static string SkillNotRegistered(string skillName) => Format(nameof(SkillNotRegistered), skillName);
    public static string SkillAssetTypeUnsupported(string skillName, object assetType) => Format(nameof(SkillAssetTypeUnsupported), skillName, assetType);
    public static string HumanReviewRequiredIssue => GetRequiredResource(nameof(HumanReviewRequiredIssue));
    public static string HumanReviewReason => GetRequiredResource(nameof(HumanReviewReason));
    public static string HumanReviewTaskNotFound(Guid taskId) => Format(nameof(HumanReviewTaskNotFound), taskId);
    public static string HumanReviewTaskCannotStart(Guid taskId, string status) => Format(nameof(HumanReviewTaskCannotStart), taskId, status);
    public static string HumanReviewTaskCannotComplete(Guid taskId, string status) => Format(nameof(HumanReviewTaskCannotComplete), taskId, status);
    public static string HumanReviewDecisionRequired => GetRequiredResource(nameof(HumanReviewDecisionRequired));
    public static string HumanReviewDecisionUnsupported(string decision) => Format(nameof(HumanReviewDecisionUnsupported), decision);
    public static string HumanReviewCorrectionsRequired => GetRequiredResource(nameof(HumanReviewCorrectionsRequired));
    public static string HumanReviewResultMissing(Guid taskId) => Format(nameof(HumanReviewResultMissing), taskId);
    public static string HumanReviewRejectedResult => GetRequiredResource(nameof(HumanReviewRejectedResult));
    public static string WebhookTargetKeyRequired => GetRequiredResource(nameof(WebhookTargetKeyRequired));
    public static string WebhookTargetUrlRequired => GetRequiredResource(nameof(WebhookTargetUrlRequired));
    public static string WebhookTargetUrlInvalid => GetRequiredResource(nameof(WebhookTargetUrlInvalid));
    public static string WebhookSigningSecretRequired => GetRequiredResource(nameof(WebhookSigningSecretRequired));
    public static string WebhookTargetAlreadyExists(string targetKey, string clientKey) => Format(nameof(WebhookTargetAlreadyExists), targetKey, clientKey);
    public static string AnalysisJobQueuedLogMessage => GetRequiredResource(nameof(AnalysisJobQueuedLogMessage));
    public static string AnalysisJobProcessingStartedLogMessage => GetRequiredResource(nameof(AnalysisJobProcessingStartedLogMessage));
    public static string AnalysisJobProcessingCompletedLogMessage => GetRequiredResource(nameof(AnalysisJobProcessingCompletedLogMessage));
    public static string AnalysisJobProcessingRetryLogMessage => GetRequiredResource(nameof(AnalysisJobProcessingRetryLogMessage));
    public static string AnalysisJobProcessingFailedLogMessage => GetRequiredResource(nameof(AnalysisJobProcessingFailedLogMessage));
    public static string RetentionDaysMustBePositive => GetRequiredResource(nameof(RetentionDaysMustBePositive));
    public static string RetentionPolicyNotFound(string clientKey) => Format(nameof(RetentionPolicyNotFound), clientKey);
    public static string RetentionPolicyDisabled(string clientKey) => Format(nameof(RetentionPolicyDisabled), clientKey);
    public static string CapabilityResourceMissing(string name) => Format(nameof(CapabilityResourceMissing), name);
    public static string ClientDisplayNameRequired => GetRequiredResource(nameof(ClientDisplayNameRequired));
    public static string ClientAlreadyExists(string clientKey) => Format(nameof(ClientAlreadyExists), clientKey);
    public static string ClientNotFound(string clientKey) => Format(nameof(ClientNotFound), clientKey);
    public static string ApiKeyNotFound(Guid apiKeyId) => Format(nameof(ApiKeyNotFound), apiKeyId);
    public static string ApiKeyClientMismatch => GetRequiredResource(nameof(ApiKeyClientMismatch));
    public static string ApiKeyAlreadyRevoked => GetRequiredResource(nameof(ApiKeyAlreadyRevoked));
    public static string ArenysmedDeliveryEvidenceDescription => GetRequiredResource(nameof(ArenysmedDeliveryEvidenceDescription));
    public static string SportsEventClipsDescription => GetRequiredResource(nameof(SportsEventClipsDescription));
    public static string GenericDocumentExtractionDescription => GetRequiredResource(nameof(GenericDocumentExtractionDescription));
    public static string GenericDocumentOcrDescription => GetRequiredResource(nameof(GenericDocumentOcrDescription));
    public static string InvoiceExtractionDescription => GetRequiredResource(nameof(InvoiceExtractionDescription));
    public static string EventVideoTargetSearchDescription => GetRequiredResource(nameof(EventVideoTargetSearchDescription));
    public static string EventFaceSearchDescription => GetRequiredResource(nameof(EventFaceSearchDescription));
    public static string ImageBatchSearchDescription => GetRequiredResource(nameof(ImageBatchSearchDescription));
    public static string VideoSearchProfileRequired(string profileKey) => Format(nameof(VideoSearchProfileRequired), profileKey);
    public static string VideoSearchVideosRequired => GetRequiredResource(nameof(VideoSearchVideosRequired));
    public static string VideoSearchMaxVideosExceeded(int maxVideos) => Format(nameof(VideoSearchMaxVideosExceeded), maxVideos);
    public static string VideoSearchMaxDurationExceeded(int maxDurationSeconds) => Format(nameof(VideoSearchMaxDurationExceeded), maxDurationSeconds);
    public static string VideoSearchManifestRequired => GetRequiredResource(nameof(VideoSearchManifestRequired));
    public static string VideoSearchManifestMustBeObject => GetRequiredResource(nameof(VideoSearchManifestMustBeObject));
    public static string VideoSearchManifestPropertyUnsupported(string propertyName) => Format(nameof(VideoSearchManifestPropertyUnsupported), propertyName);
    public static string VideoSearchPersonalDataNotAllowed(string propertyName) => Format(nameof(VideoSearchPersonalDataNotAllowed), propertyName);
    public static string VideoSearchAtLeastOneDetectionRequired => GetRequiredResource(nameof(VideoSearchAtLeastOneDetectionRequired));
    public static string VideoSearchMinConfidenceInvalid => GetRequiredResource(nameof(VideoSearchMinConfidenceInvalid));
    public static string VideoSearchMergeGapInvalid => GetRequiredResource(nameof(VideoSearchMergeGapInvalid));
    public static string VideoSearchTargetsRequired => GetRequiredResource(nameof(VideoSearchTargetsRequired));
    public static string VideoSearchTargetMustBeObject => GetRequiredResource(nameof(VideoSearchTargetMustBeObject));
    public static string VideoSearchTargetKeyRequired => GetRequiredResource(nameof(VideoSearchTargetKeyRequired));
    public static string VideoSearchVisibleNumberRequired => GetRequiredResource(nameof(VideoSearchVisibleNumberRequired));
    public static string VideoSearchVisibleNumberMustContainDigit => GetRequiredResource(nameof(VideoSearchVisibleNumberMustContainDigit));
    public static string VideoSearchJobQueuedLogMessage => GetRequiredResource(nameof(VideoSearchJobQueuedLogMessage));
    public static string VideoSearchJobProcessingRetryLogMessage => GetRequiredResource(nameof(VideoSearchJobProcessingRetryLogMessage));
    public static string VideoSearchJobProcessingFailedLogMessage => GetRequiredResource(nameof(VideoSearchJobProcessingFailedLogMessage));
    public static string VideoSearchJobProcessingCompletedLogMessage => GetRequiredResource(nameof(VideoSearchJobProcessingCompletedLogMessage));
    public static string FaceSearchProfileRequired(string profileKey) => Format(nameof(FaceSearchProfileRequired), profileKey);
    public static string FaceSearchVideosRequired => GetRequiredResource(nameof(FaceSearchVideosRequired));
    public static string FaceSearchReferenceImagesRequired => GetRequiredResource(nameof(FaceSearchReferenceImagesRequired));
    public static string FaceSearchManifestRequired => GetRequiredResource(nameof(FaceSearchManifestRequired));
    public static string FaceSearchManifestMustBeObject => GetRequiredResource(nameof(FaceSearchManifestMustBeObject));
    public static string FaceSearchManifestPropertyUnsupported(string propertyName) => Format(nameof(FaceSearchManifestPropertyUnsupported), propertyName);
    public static string FaceSearchPersonalDataNotAllowed(string propertyName) => Format(nameof(FaceSearchPersonalDataNotAllowed), propertyName);
    public static string FaceSearchMinSimilarityInvalid => GetRequiredResource(nameof(FaceSearchMinSimilarityInvalid));
    public static string FaceSearchTargetsRequired => GetRequiredResource(nameof(FaceSearchTargetsRequired));
    public static string FaceSearchTargetMustBeObject => GetRequiredResource(nameof(FaceSearchTargetMustBeObject));
    public static string FaceSearchTargetKeyRequired => GetRequiredResource(nameof(FaceSearchTargetKeyRequired));
    public static string FaceSearchReferenceImageKeyRequired => GetRequiredResource(nameof(FaceSearchReferenceImageKeyRequired));
    public static string FaceSearchTargetReferenceImageMissing(string referenceImageKey) => Format(nameof(FaceSearchTargetReferenceImageMissing), referenceImageKey);
    public static string FaceSearchJobQueuedLogMessage => GetRequiredResource(nameof(FaceSearchJobQueuedLogMessage));
    public static string FaceSearchJobProcessingRetryLogMessage => GetRequiredResource(nameof(FaceSearchJobProcessingRetryLogMessage));
    public static string FaceSearchJobProcessingFailedLogMessage => GetRequiredResource(nameof(FaceSearchJobProcessingFailedLogMessage));
    public static string FaceSearchJobProcessingCompletedLogMessage => GetRequiredResource(nameof(FaceSearchJobProcessingCompletedLogMessage));
    public static string ImageBatchSearchProfileRequired(string profileKey) => Format(nameof(ImageBatchSearchProfileRequired), profileKey);
    public static string ImageBatchSearchImagesRequired => GetRequiredResource(nameof(ImageBatchSearchImagesRequired));
    public static string ImageBatchSearchReferenceImagesRequired => GetRequiredResource(nameof(ImageBatchSearchReferenceImagesRequired));
    public static string ImageBatchSearchMaxImagesExceeded(int maxImages) => Format(nameof(ImageBatchSearchMaxImagesExceeded), maxImages);
    public static string ImageBatchSearchMaxTargetsExceeded(int maxTargets) => Format(nameof(ImageBatchSearchMaxTargetsExceeded), maxTargets);
    public static string ImageBatchSearchManifestRequired => GetRequiredResource(nameof(ImageBatchSearchManifestRequired));
    public static string ImageBatchSearchManifestMustBeObject => GetRequiredResource(nameof(ImageBatchSearchManifestMustBeObject));
    public static string ImageBatchSearchManifestPropertyUnsupported(string propertyName) => Format(nameof(ImageBatchSearchManifestPropertyUnsupported), propertyName);
    public static string ImageBatchSearchPersonalDataNotAllowed(string propertyName) => Format(nameof(ImageBatchSearchPersonalDataNotAllowed), propertyName);
    public static string ImageBatchSearchTargetsRequired => GetRequiredResource(nameof(ImageBatchSearchTargetsRequired));
    public static string ImageBatchSearchTargetMustBeObject => GetRequiredResource(nameof(ImageBatchSearchTargetMustBeObject));
    public static string ImageBatchSearchTargetKeyRequired => GetRequiredResource(nameof(ImageBatchSearchTargetKeyRequired));
    public static string ImageBatchSearchQueryTypeRequired => GetRequiredResource(nameof(ImageBatchSearchQueryTypeRequired));
    public static string ImageBatchSearchQueryTypeUnsupported(string queryType) => Format(nameof(ImageBatchSearchQueryTypeUnsupported), queryType);
    public static string ImageBatchSearchVisibleNumberRequired => GetRequiredResource(nameof(ImageBatchSearchVisibleNumberRequired));
    public static string ImageBatchSearchVisibleNumberMustContainDigit => GetRequiredResource(nameof(ImageBatchSearchVisibleNumberMustContainDigit));
    public static string ImageBatchSearchReferenceImageKeyRequired => GetRequiredResource(nameof(ImageBatchSearchReferenceImageKeyRequired));
    public static string ImageBatchSearchTargetReferenceImageMissing(string referenceImageKey) => Format(nameof(ImageBatchSearchTargetReferenceImageMissing), referenceImageKey);
    public static string ImageBatchSearchMinConfidenceInvalid => GetRequiredResource(nameof(ImageBatchSearchMinConfidenceInvalid));
    public static string ImageBatchSearchMinSimilarityInvalid => GetRequiredResource(nameof(ImageBatchSearchMinSimilarityInvalid));
    public static string ImageBatchSearchProvidersMustBeArray => GetRequiredResource(nameof(ImageBatchSearchProvidersMustBeArray));
    public static string ImageBatchSearchJobQueuedLogMessage => GetRequiredResource(nameof(ImageBatchSearchJobQueuedLogMessage));
    public static string ImageBatchSearchJobProcessingRetryLogMessage => GetRequiredResource(nameof(ImageBatchSearchJobProcessingRetryLogMessage));
    public static string ImageBatchSearchJobProcessingFailedLogMessage => GetRequiredResource(nameof(ImageBatchSearchJobProcessingFailedLogMessage));
    public static string ImageBatchSearchJobProcessingCompletedLogMessage => GetRequiredResource(nameof(ImageBatchSearchJobProcessingCompletedLogMessage));
    public static string CloudAssetSelectorRequired => GetRequiredResource(nameof(CloudAssetSelectorRequired));
    public static string CloudAssetSelectorMustBeObject => GetRequiredResource(nameof(CloudAssetSelectorMustBeObject));
    public static string CloudAssetSelectorModeRequired => GetRequiredResource(nameof(CloudAssetSelectorModeRequired));
    public static string CloudAssetSelectorModeUnsupported(string selectorMode) => Format(nameof(CloudAssetSelectorModeUnsupported), selectorMode);
    public static string CloudAssetStorageProviderRequired => GetRequiredResource(nameof(CloudAssetStorageProviderRequired));
    public static string CloudAssetStorageProviderUnsupported(string storageProvider) => Format(nameof(CloudAssetStorageProviderUnsupported), storageProvider);
    public static string CloudAssetBucketRequired => GetRequiredResource(nameof(CloudAssetBucketRequired));
    public static string CloudAssetPrefixRequired => GetRequiredResource(nameof(CloudAssetPrefixRequired));
    public static string CloudAssetExplicitAssetsRequired => GetRequiredResource(nameof(CloudAssetExplicitAssetsRequired));
    public static string CloudAssetMustBeObject => GetRequiredResource(nameof(CloudAssetMustBeObject));
    public static string CloudAssetKeyRequired => GetRequiredResource(nameof(CloudAssetKeyRequired));
    public static string CloudAssetObjectKeyRequired => GetRequiredResource(nameof(CloudAssetObjectKeyRequired));
    public static string CloudAssetUrlsNotAllowed => GetRequiredResource(nameof(CloudAssetUrlsNotAllowed));
    public static string CloudAssetBucketNotAllowed(string bucketName) => Format(nameof(CloudAssetBucketNotAllowed), bucketName);
    public static string CloudAssetMaxAssetsExceeded(int maxAssets) => Format(nameof(CloudAssetMaxAssetsExceeded), maxAssets);
    public static string CloudAssetNoAssetsResolved => GetRequiredResource(nameof(CloudAssetNoAssetsResolved));

    private static string GetRequiredResource(string name)
    {
        return Resources.GetString(name)
            ?? throw new InvalidOperationException($"Missing application resource '{name}'.");
    }

    private static string Format(string name, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, GetRequiredResource(name), args);
    }
}
