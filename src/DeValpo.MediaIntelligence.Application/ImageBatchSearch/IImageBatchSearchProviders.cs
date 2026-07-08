using DeValpo.MediaIntelligence.Application.ImageBatchSearch.Models;

namespace DeValpo.MediaIntelligence.Application.ImageBatchSearch;

public interface IImageBatchTextSearchProvider
{
    /// <summary>
    /// Stable provider name used in persisted results and API responses.
    /// </summary>
    string SourceProvider { get; }

    /// <summary>
    /// Runs OCR-based target search over the uploaded image batch.
    /// </summary>
    Task<ImageBatchProviderResult> SearchTextAsync(
        ImageBatchTextSearchRequest request,
        CancellationToken cancellationToken);
}

public interface IImageBatchFaceSearchProvider
{
    /// <summary>
    /// Stable provider name used in persisted results and API responses.
    /// </summary>
    string SourceProvider { get; }

    /// <summary>
    /// Runs face-reference target search over the uploaded image batch.
    /// </summary>
    Task<ImageBatchProviderResult> SearchFacesAsync(
        ImageBatchFaceSearchRequest request,
        CancellationToken cancellationToken);
}
