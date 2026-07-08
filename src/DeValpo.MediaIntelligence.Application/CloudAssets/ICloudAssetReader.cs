namespace DeValpo.MediaIntelligence.Application.CloudAssets;

public interface ICloudAssetReader
{
    /// <summary>
    /// Opens an immutable cloud object for provider staging when cross-cloud processing is required.
    /// </summary>
    Task<Stream> OpenReadAsync(
        string storageProvider,
        string bucketName,
        string objectKey,
        string? versionId,
        string? generation,
        CancellationToken cancellationToken);
}
