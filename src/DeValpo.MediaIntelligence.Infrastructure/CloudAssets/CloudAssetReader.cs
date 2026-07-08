using Amazon.S3.Model;
using DeValpo.MediaIntelligence.Application.CloudAssets;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Google.Vision;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.CloudAssets;

public sealed class CloudAssetReader(
    IAwsRekognitionClientFactory awsClientFactory,
    IOptions<AwsRekognitionOptions> awsOptions,
    IOptions<GoogleVisionOptions> googleVisionOptions) : ICloudAssetReader
{
    /// <summary>
    /// Opens an object from S3 or GCS for provider staging.
    /// </summary>
    public async Task<Stream> OpenReadAsync(
        string storageProvider,
        string bucketName,
        string objectKey,
        string? versionId,
        string? generation,
        CancellationToken cancellationToken)
    {
        if (string.Equals(storageProvider, CloudStorageProviders.S3, StringComparison.OrdinalIgnoreCase))
        {
            var client = awsClientFactory.CreateS3Client(awsOptions.Value);
            var response = await client.GetObjectAsync(
                new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    VersionId = versionId
                },
                cancellationToken);

            return new DisposableClientStream(response.ResponseStream, response, client);
        }

        if (string.Equals(storageProvider, CloudStorageProviders.Gcs, StringComparison.OrdinalIgnoreCase))
        {
            var credential = await GoogleVisionCredentialProvider.GetCredentialAsync(googleVisionOptions.Value, cancellationToken);
            var client = await StorageClient.CreateAsync(credential);
            var tempDirectory = Path.Combine(Path.GetTempPath(), "mediaintelligence-cloud-assets");
            Directory.CreateDirectory(tempDirectory);
            var tempPath = Path.Combine(tempDirectory, $"{Guid.NewGuid():N}.asset");
            var stream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.Read,
                bufferSize: 1024 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);
            await client.DownloadObjectAsync(
                bucketName,
                objectKey,
                stream,
                new DownloadObjectOptions { Generation = ToLong(generation) },
                cancellationToken);
            stream.Position = 0;
            return stream;
        }

        throw new ArgumentException($"Unsupported storage provider '{storageProvider}'.", nameof(storageProvider));
    }

    private static long? ToLong(string? value)
    {
        return long.TryParse(value, out var parsed) ? parsed : null;
    }

    private sealed class DisposableClientStream(Stream inner, IDisposable response, IDisposable client) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
                response.Dispose();
                client.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
