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
    public static string CapabilityResourceMissing(string name) => Format(nameof(CapabilityResourceMissing), name);
    public static string ClientDisplayNameRequired => GetRequiredResource(nameof(ClientDisplayNameRequired));
    public static string ClientAlreadyExists(string clientKey) => Format(nameof(ClientAlreadyExists), clientKey);
    public static string ApiKeyNotFound(Guid apiKeyId) => Format(nameof(ApiKeyNotFound), apiKeyId);
    public static string ApiKeyClientMismatch => GetRequiredResource(nameof(ApiKeyClientMismatch));
    public static string ApiKeyAlreadyRevoked => GetRequiredResource(nameof(ApiKeyAlreadyRevoked));
    public static string ArenysmedDeliveryEvidenceDescription => GetRequiredResource(nameof(ArenysmedDeliveryEvidenceDescription));
    public static string SportsEventClipsDescription => GetRequiredResource(nameof(SportsEventClipsDescription));
    public static string GenericDocumentExtractionDescription => GetRequiredResource(nameof(GenericDocumentExtractionDescription));
    public static string GenericDocumentOcrDescription => GetRequiredResource(nameof(GenericDocumentOcrDescription));
    public static string InvoiceExtractionDescription => GetRequiredResource(nameof(InvoiceExtractionDescription));

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
