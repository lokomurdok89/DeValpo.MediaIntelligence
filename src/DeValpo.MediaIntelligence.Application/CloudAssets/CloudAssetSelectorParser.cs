using System.Text.Json;
using DeValpo.MediaIntelligence.Application.Resources;

namespace DeValpo.MediaIntelligence.Application.CloudAssets;

public static class CloudAssetSelectorParser
{
    private static readonly HashSet<string> UrlProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "url", "uri", "signedUrl", "publicUrl", "downloadUrl"
    };

    /// <summary>
    /// Parses the cloud-reference selector used by production jobs.
    /// </summary>
    public static CloudAssetSelector? ParseOptional(string? selectorJson)
    {
        if (string.IsNullOrWhiteSpace(selectorJson))
        {
            return null;
        }

        using var document = JsonDocument.Parse(selectorJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(ApplicationResourceText.CloudAssetSelectorMustBeObject);
        }

        RejectUrls(document.RootElement);

        var selectorMode = GetRequiredString(document.RootElement, "selectorMode", ApplicationResourceText.CloudAssetSelectorModeRequired);
        if (!IsSupportedSelectorMode(selectorMode))
        {
            throw new ArgumentException(ApplicationResourceText.CloudAssetSelectorModeUnsupported(selectorMode));
        }

        var storageProvider = NormalizeStorageProvider(GetRequiredString(
            document.RootElement,
            "storageProvider",
            ApplicationResourceText.CloudAssetStorageProviderRequired));
        var bucketName = GetRequiredString(document.RootElement, "bucketName", ApplicationResourceText.CloudAssetBucketRequired);
        var prefix = GetOptionalString(document.RootElement, "prefix");
        var region = GetOptionalString(document.RootElement, "region");
        var location = GetOptionalString(document.RootElement, "location");

        var assets = ReadAssets(document.RootElement, storageProvider, bucketName, region, location);
        if (string.Equals(selectorMode, CloudAssetSelectorModes.AllPrefix, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException(ApplicationResourceText.CloudAssetPrefixRequired);
        }

        if (string.Equals(selectorMode, CloudAssetSelectorModes.ExplicitAssets, StringComparison.OrdinalIgnoreCase) &&
            assets.Count == 0)
        {
            throw new ArgumentException(ApplicationResourceText.CloudAssetExplicitAssetsRequired);
        }

        return new CloudAssetSelector(
            selectorMode,
            storageProvider,
            bucketName.Trim(),
            prefix?.Trim(),
            region?.Trim(),
            location?.Trim(),
            assets);
    }

    private static IReadOnlyCollection<CloudAssetReference> ReadAssets(
        JsonElement root,
        string storageProvider,
        string bucketName,
        string? region,
        string? location)
    {
        if (!root.TryGetProperty("assets", out var assetsElement) ||
            assetsElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        if (assetsElement.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException(ApplicationResourceText.CloudAssetExplicitAssetsRequired);
        }

        var assets = new List<CloudAssetReference>();
        foreach (var assetElement in assetsElement.EnumerateArray())
        {
            if (assetElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException(ApplicationResourceText.CloudAssetMustBeObject);
            }

            RejectUrls(assetElement);

            var objectKey = GetRequiredString(assetElement, "objectKey", ApplicationResourceText.CloudAssetObjectKeyRequired);
            assets.Add(new CloudAssetReference(
                GetRequiredString(assetElement, "assetKey", ApplicationResourceText.CloudAssetKeyRequired),
                storageProvider,
                GetOptionalString(assetElement, "bucketName") ?? bucketName,
                objectKey,
                GetOptionalString(assetElement, "region") ?? region,
                GetOptionalString(assetElement, "location") ?? location,
                GetOptionalString(assetElement, "contentType"),
                GetOptionalLong(assetElement, "sizeInBytes"),
                GetOptionalString(assetElement, "checksum"),
                GetOptionalString(assetElement, "versionId"),
                GetOptionalString(assetElement, "generation"),
                GetOptionalString(assetElement, "fileName")));
        }

        return assets;
    }

    private static void RejectUrls(JsonElement element)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (UrlProperties.Contains(property.Name))
            {
                throw new ArgumentException(ApplicationResourceText.CloudAssetUrlsNotAllowed);
            }
        }
    }

    private static bool IsSupportedSelectorMode(string selectorMode)
    {
        return string.Equals(selectorMode, CloudAssetSelectorModes.AllPrefix, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(selectorMode, CloudAssetSelectorModes.ExplicitAssets, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(selectorMode, CloudAssetSelectorModes.MultipartUpload, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeStorageProvider(string storageProvider)
    {
        if (string.Equals(storageProvider, CloudStorageProviders.S3, StringComparison.OrdinalIgnoreCase))
        {
            return CloudStorageProviders.S3;
        }

        if (string.Equals(storageProvider, CloudStorageProviders.Gcs, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(storageProvider, "GoogleCloudStorage", StringComparison.OrdinalIgnoreCase))
        {
            return CloudStorageProviders.Gcs;
        }

        throw new ArgumentException(ApplicationResourceText.CloudAssetStorageProviderUnsupported(storageProvider));
    }

    private static string GetRequiredString(JsonElement element, string propertyName, string errorMessage)
    {
        var value = GetOptionalString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(errorMessage);
        }

        return value.Trim();
    }

    private static string? GetOptionalString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static long? GetOptionalLong(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.TryGetInt64(out var value)
            ? value
            : null;
    }
}
