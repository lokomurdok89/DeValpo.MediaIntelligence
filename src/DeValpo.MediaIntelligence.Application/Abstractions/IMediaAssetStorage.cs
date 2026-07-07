using DeValpo.MediaIntelligence.Application.Models;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IMediaAssetStorage
{
    /// <summary>
    /// Persists the incoming media stream and returns its storage metadata.
    /// </summary>
    Task<StoredMediaAsset> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Opens a previously stored media asset for read-only processing.
    /// </summary>
    Task<Stream> OpenReadAsync(
        string storagePath,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a stored media asset when it exists.
    /// </summary>
    Task<bool> DeleteAsync(
        string storagePath,
        CancellationToken cancellationToken);
}
