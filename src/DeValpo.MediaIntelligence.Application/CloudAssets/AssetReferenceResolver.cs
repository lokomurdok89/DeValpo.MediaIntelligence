using DeValpo.MediaIntelligence.Application.Resources;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Application.CloudAssets;

public sealed class AssetReferenceResolver(
    IEnumerable<ICloudAssetBrowser> browsers,
    IOptions<CloudAssetOptions> options) : IAssetReferenceResolver
{
    /// <summary>
    /// Resolves cloud selectors while enforcing bucket allow-lists and asset-count limits.
    /// </summary>
    public async Task<IReadOnlyCollection<ResolvedJobAsset>> ResolveAsync(
        CloudAssetSelector selector,
        int maxAssets,
        CancellationToken cancellationToken)
    {
        ValidateBucketAllowed(selector);

        var browser = browsers.FirstOrDefault(item =>
            string.Equals(item.StorageProvider, selector.StorageProvider, StringComparison.OrdinalIgnoreCase));
        if (browser is null)
        {
            throw new ArgumentException(ApplicationResourceText.CloudAssetStorageProviderUnsupported(selector.StorageProvider));
        }

        var effectiveMaxAssets = Math.Min(maxAssets, Math.Max(options.Value.MaxAssetsPerJob, 1));
        var assets = string.Equals(selector.SelectorMode, CloudAssetSelectorModes.AllPrefix, StringComparison.OrdinalIgnoreCase)
            ? await browser.ListByPrefixAsync(selector, effectiveMaxAssets, cancellationToken)
            : await browser.ResolveExplicitAsync(selector, effectiveMaxAssets, cancellationToken);

        if (assets.Count == 0)
        {
            throw new ArgumentException(ApplicationResourceText.CloudAssetNoAssetsResolved);
        }

        if (assets.Count > effectiveMaxAssets)
        {
            throw new ArgumentException(ApplicationResourceText.CloudAssetMaxAssetsExceeded(effectiveMaxAssets));
        }

        return assets;
    }

    private void ValidateBucketAllowed(CloudAssetSelector selector)
    {
        var allowedBuckets = string.Equals(selector.StorageProvider, CloudStorageProviders.S3, StringComparison.OrdinalIgnoreCase)
            ? options.Value.AllowedS3Buckets
            : options.Value.AllowedGcsBuckets;

        if (allowedBuckets.Length == 0 ||
            !allowedBuckets.Contains(selector.BucketName, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(ApplicationResourceText.CloudAssetBucketNotAllowed(selector.BucketName));
        }

        foreach (var asset in selector.Assets)
        {
            if (!allowedBuckets.Contains(asset.BucketName, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(ApplicationResourceText.CloudAssetBucketNotAllowed(asset.BucketName));
            }
        }
    }
}
