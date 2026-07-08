namespace DeValpo.MediaIntelligence.Application.CloudAssets;

public interface ICloudAssetBrowser
{
    string StorageProvider { get; }

    /// <summary>
    /// Lists all supported assets under a cloud storage prefix.
    /// </summary>
    Task<IReadOnlyCollection<ResolvedJobAsset>> ListByPrefixAsync(
        CloudAssetSelector selector,
        int maxAssets,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves explicit object references into complete asset metadata.
    /// </summary>
    Task<IReadOnlyCollection<ResolvedJobAsset>> ResolveExplicitAsync(
        CloudAssetSelector selector,
        int maxAssets,
        CancellationToken cancellationToken);
}
