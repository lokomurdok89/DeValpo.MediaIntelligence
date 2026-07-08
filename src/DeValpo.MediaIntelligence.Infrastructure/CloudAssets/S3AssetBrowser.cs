using Amazon.S3.Model;
using DeValpo.MediaIntelligence.Application.CloudAssets;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.CloudAssets;

public sealed class S3AssetBrowser(
    IAwsRekognitionClientFactory clientFactory,
    IOptions<AwsRekognitionOptions> options) : ICloudAssetBrowser
{
    public string StorageProvider => CloudStorageProviders.S3;

    /// <summary>
    /// Lists S3 objects under the requested prefix and converts them into job asset snapshots.
    /// </summary>
    public async Task<IReadOnlyCollection<ResolvedJobAsset>> ListByPrefixAsync(
        CloudAssetSelector selector,
        int maxAssets,
        CancellationToken cancellationToken)
    {
        using var client = clientFactory.CreateS3Client(options.Value);
        var assets = new List<ResolvedJobAsset>();
        string? continuationToken = null;
        do
        {
            var response = await client.ListObjectsV2Async(
                new ListObjectsV2Request
                {
                    BucketName = selector.BucketName,
                    Prefix = selector.Prefix,
                    ContinuationToken = continuationToken,
                    MaxKeys = Math.Min(maxAssets + 1, 1000)
                },
                cancellationToken);

            foreach (var item in response.S3Objects.Where(item => item.Size.GetValueOrDefault() > 0 && !item.Key.EndsWith("/", StringComparison.Ordinal)))
            {
                assets.Add(ToResolved(selector, item.Key, item.Size.GetValueOrDefault(), item.ETag, contentType: null, versionId: null));
                if (assets.Count > maxAssets)
                {
                    return assets;
                }
            }

            continuationToken = response.IsTruncated.GetValueOrDefault() ? response.NextContinuationToken : null;
        }
        while (!string.IsNullOrWhiteSpace(continuationToken));

        return assets;
    }

    /// <summary>
    /// Resolves explicit S3 object references and enriches them with object metadata when needed.
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

        using var client = clientFactory.CreateS3Client(options.Value);
        var assets = new List<ResolvedJobAsset>();
        foreach (var asset in selector.Assets)
        {
            var metadata = await client.GetObjectMetadataAsync(
                new GetObjectMetadataRequest
                {
                    BucketName = asset.BucketName,
                    Key = asset.ObjectKey,
                    VersionId = asset.VersionId
                },
                cancellationToken);

            assets.Add(new ResolvedJobAsset(
                asset.AssetKey.Trim(),
                CloudStorageProviders.S3,
                asset.BucketName.Trim(),
                asset.ObjectKey.Trim(),
                asset.Region,
                asset.Location,
                string.IsNullOrWhiteSpace(asset.ContentType) ? metadata.Headers.ContentType ?? InferContentType(asset.ObjectKey) : asset.ContentType,
                asset.SizeInBytes ?? metadata.ContentLength,
                asset.Checksum ?? metadata.ETag,
                asset.VersionId,
                asset.Generation,
                string.IsNullOrWhiteSpace(asset.FileName) ? Path.GetFileName(asset.ObjectKey) : asset.FileName.Trim()));
        }

        return assets;
    }

    private static ResolvedJobAsset ToResolved(CloudAssetSelector selector, string objectKey, long size, string? checksum, string? contentType, string? versionId)
    {
        return new ResolvedJobAsset(
            BuildAssetKey(objectKey),
            CloudStorageProviders.S3,
            selector.BucketName,
            objectKey,
            selector.Region,
            selector.Location,
            contentType ?? InferContentType(objectKey),
            size,
            checksum,
            versionId,
            null,
            Path.GetFileName(objectKey));
    }

    private static ResolvedJobAsset ToResolvedWithoutMetadata(CloudAssetReference asset)
    {
        return new ResolvedJobAsset(
            asset.AssetKey.Trim(),
            CloudStorageProviders.S3,
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
