namespace DeValpo.MediaIntelligence.Application.CloudAssets;

public interface ICloudStagingWriter
{
    /// <summary>
    /// Stages an object into a provider-owned temporary bucket without deleting it at job completion.
    /// </summary>
    Task<StagingReference> StageAsync(
        string storageProvider,
        string bucketName,
        string prefix,
        Guid jobId,
        string objectKey,
        string contentType,
        Stream content,
        CancellationToken cancellationToken);
}
