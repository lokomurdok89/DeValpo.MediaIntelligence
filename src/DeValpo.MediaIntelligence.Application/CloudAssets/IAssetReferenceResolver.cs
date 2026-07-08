namespace DeValpo.MediaIntelligence.Application.CloudAssets;

public interface IAssetReferenceResolver
{
    /// <summary>
    /// Resolves a product-facing cloud selector into immutable asset snapshots for a job.
    /// </summary>
    Task<IReadOnlyCollection<ResolvedJobAsset>> ResolveAsync(
        CloudAssetSelector selector,
        int maxAssets,
        CancellationToken cancellationToken);
}
