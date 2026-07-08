using DeValpo.MediaIntelligence.Application.CloudAssets;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Google.Vision;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.CloudAssets;

public sealed class GcsAssetBrowser(
    IOptions<GoogleVisionOptions> options) : ICloudAssetBrowser
{
    public string StorageProvider => CloudStorageProviders.Gcs;

    /// <summary>
    /// Lists GCS objects under the requested prefix and converts them into job asset snapshots.
    /// </summary>
    public async Task<IReadOnlyCollection<ResolvedJobAsset>> ListByPrefixAsync(
        CloudAssetSelector selector,
        int maxAssets,
        CancellationToken cancellationToken)
    {
        var client = await CreateClientAsync(cancellationToken);
        var assets = new List<ResolvedJobAsset>();
        foreach (var item in client.ListObjects(selector.BucketName, selector.Prefix))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item.Size == 0 || item.Name.EndsWith("/", StringComparison.Ordinal))
            {
                continue;
            }

            assets.Add(ToResolved(selector, item));
            if (assets.Count > maxAssets)
            {
                return assets;
            }
        }

        return assets;
    }

    /// <summary>
    /// Resolves explicit GCS object references and enriches them with object metadata.
    /// </summary>
    public async Task<IReadOnlyCollection<ResolvedJobAsset>> ResolveExplicitAsync(
        CloudAssetSelector selector,
        int maxAssets,
        CancellationToken cancellationToken)
    {
        if (selector.Assets.Count > maxAssets)
        {
            return selector.Assets.Select(ToResolvedWithoutMetadata).ToArray();
        }

        var client = await CreateClientAsync(cancellationToken);
        var assets = new List<ResolvedJobAsset>();
        foreach (var asset in selector.Assets)
        {
            var item = await client.GetObjectAsync(
                asset.BucketName,
                asset.ObjectKey,
                new GetObjectOptions { Generation = ToLong(asset.Generation) },
                cancellationToken);

            assets.Add(new ResolvedJobAsset(
                asset.AssetKey.Trim(),
                CloudStorageProviders.Gcs,
                asset.BucketName.Trim(),
                asset.ObjectKey.Trim(),
                asset.Region,
                asset.Location,
                string.IsNullOrWhiteSpace(asset.ContentType) ? item.ContentType ?? InferContentType(asset.ObjectKey) : asset.ContentType,
                asset.SizeInBytes ?? (long)item.Size.GetValueOrDefault(),
                asset.Checksum ?? item.Md5Hash,
                asset.VersionId,
                asset.Generation ?? item.Generation?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                string.IsNullOrWhiteSpace(asset.FileName) ? Path.GetFileName(asset.ObjectKey) : asset.FileName.Trim()));
        }

        return assets;
    }

    private async Task<StorageClient> CreateClientAsync(CancellationToken cancellationToken)
    {
        var credential = await GoogleVisionCredentialProvider.GetCredentialAsync(options.Value, cancellationToken);
        return await StorageClient.CreateAsync(credential);
    }

    private static ResolvedJobAsset ToResolved(CloudAssetSelector selector, Google.Apis.Storage.v1.Data.Object item)
    {
        return new ResolvedJobAsset(
            BuildAssetKey(item.Name),
            CloudStorageProviders.Gcs,
            selector.BucketName,
            item.Name,
            selector.Region,
            selector.Location,
            item.ContentType ?? InferContentType(item.Name),
            (long)item.Size.GetValueOrDefault(),
            item.Md5Hash,
            null,
            item.Generation?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Path.GetFileName(item.Name));
    }

    private static ResolvedJobAsset ToResolvedWithoutMetadata(CloudAssetReference asset)
    {
        return new ResolvedJobAsset(
            asset.AssetKey.Trim(),
            CloudStorageProviders.Gcs,
            asset.BucketName.Trim(),
            asset.ObjectKey.Trim(),
            asset.Region,
            asset.Location,
            string.IsNullOrWhiteSpace(asset.ContentType) ? InferContentType(asset.ObjectKey) : asset.ContentType,
            asset.SizeInBytes ?? 0,
            asset.Checksum,
            asset.VersionId,
            asset.Generation,
            string.IsNullOrWhiteSpace(asset.FileName) ? Path.GetFileName(asset.ObjectKey) : asset.FileName.Trim());
    }

    private static long? ToLong(string? value)
    {
        return long.TryParse(value, out var parsed) ? parsed : null;
    }

    private static string BuildAssetKey(string objectKey)
    {
        var fileName = Path.GetFileNameWithoutExtension(objectKey);
        return string.IsNullOrWhiteSpace(fileName) ? Guid.NewGuid().ToString("N") : fileName;
    }

    private static string InferContentType(string objectKey)
    {
        return Path.GetExtension(objectKey).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
