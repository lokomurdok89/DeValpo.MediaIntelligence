using DeValpo.MediaIntelligence.Application.Models;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IMediaAssetStorage
{
    Task<StoredMediaAsset> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(
        string storagePath,
        CancellationToken cancellationToken);
}
