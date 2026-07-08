using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.FaceSearch;
using DeValpo.MediaIntelligence.Application.FaceSearch.Models;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class AwsRekognitionFaceSearchProviderTests
{
    [Fact]
    public async Task AnalyzeAsync_DoesNotSendRawTargetKeyAsExternalImageId_AndPreservesResultTargetKey()
    {
        var faceId = Guid.Parse("11111111-1111-1111-1111-111111111111").ToString();
        var s3Client = new TestS3Client();
        var rekognitionClient = new TestRekognitionClient(faceId);
        var provider = CreateProvider(s3Client, rekognitionClient);

        var result = await provider.AnalyzeAsync(
            CreateRequest("target with spaces/and/slashes"),
            CancellationToken.None);

        rekognitionClient.IndexRequest.Should().NotBeNull();
        rekognitionClient.IndexRequest!.ExternalImageId.Should().BeNull();
        result.FaceMatches.Should().ContainSingle();
        result.FaceMatches.Single().TargetKey.Should().Be("target with spaces/and/slashes");
        result.FaceMatches.Single().ReferenceImageKey.Should().Be("reference-1");
    }

    [Fact]
    public async Task AnalyzeAsync_LogsCleanupFailureWithoutFailingProviderResult()
    {
        var faceId = Guid.Parse("22222222-2222-2222-2222-222222222222").ToString();
        var logger = new ListLogger<AwsRekognitionFaceSearchProvider>();
        var provider = CreateProvider(
            new TestS3Client(),
            new TestRekognitionClient(faceId) { ThrowOnDeleteFaces = true },
            logger);

        var result = await provider.AnalyzeAsync(
            CreateRequest("target-1", includeVideo: false),
            CancellationToken.None);

        result.Issues.Should().BeEmpty();
        logger.Messages.Should().Contain(message => message.Contains("temporary indexed faces", StringComparison.OrdinalIgnoreCase));
    }

    private static AwsRekognitionFaceSearchProvider CreateProvider(
        TestS3Client s3Client,
        TestRekognitionClient rekognitionClient,
        ILogger<AwsRekognitionFaceSearchProvider>? logger = null)
    {
        return new AwsRekognitionFaceSearchProvider(
            new FakeStorage(),
            Options.Create(new AwsRekognitionOptions
            {
                Enabled = true,
                FaceSearchEnabled = true,
                Region = "us-east-1",
                BucketName = "private-bucket",
                CollectionId = "face-search-test",
                PollingIntervalSeconds = 1,
                PollingTimeoutSeconds = 1,
                DeleteStagedObjectsAfterProcessing = true
            }),
            new TestClientFactory(s3Client, rekognitionClient),
            logger ?? new ListLogger<AwsRekognitionFaceSearchProvider>());
    }

    private static FaceSearchProviderRequest CreateRequest(string targetKey, bool includeVideo = true)
    {
        var videos = includeVideo
            ? new[]
            {
                new FaceSearchProviderAsset(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "video-1",
                    "race.mp4",
                    "stored/race.mp4",
                    "video/mp4")
            }
            : [];

        return new FaceSearchProviderRequest(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "consent-001",
            videos,
            [
                new FaceSearchProviderAsset(
                    Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    "reference-1",
                    "face.jpg",
                    "stored/face.jpg",
                    "image/jpeg")
            ],
            [new FaceSearchTarget(targetKey, "reference-1")],
            90m);
    }

    private sealed class FakeStorage : IMediaAssetStorage
    {
        public Task<StoredMediaAsset> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
        {
            return Task.FromResult(new StoredMediaAsset($"stored/{fileName}", fileName, contentType, content.Length));
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream>(new MemoryStream([0x00, 0x00, 0x00, 0x18]));
        }

        public Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class TestClientFactory(
        IAmazonS3 s3Client,
        IAmazonRekognition rekognitionClient) : IAwsRekognitionClientFactory
    {
        public IAmazonS3 CreateS3Client(AwsRekognitionOptions options) => s3Client;

        public IAmazonRekognition CreateRekognitionClient(AwsRekognitionOptions options) => rekognitionClient;
    }

    private sealed class TestS3Client : AmazonS3Client
    {
        public TestS3Client()
            : base(new AnonymousAWSCredentials(), RegionEndpoint.USEast1)
        {
        }

        public override Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PutObjectResponse());
        }

        public override Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DeleteObjectResponse());
        }
    }

    private sealed class TestRekognitionClient(string faceId) : AmazonRekognitionClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1)
    {
        public IndexFacesRequest? IndexRequest { get; private set; }
        public bool ThrowOnDeleteFaces { get; init; }

        public override Task<IndexFacesResponse> IndexFacesAsync(IndexFacesRequest request, CancellationToken cancellationToken = default)
        {
            IndexRequest = request;
            return Task.FromResult(new IndexFacesResponse
            {
                FaceRecords =
                [
                    new FaceRecord
                    {
                        Face = new Face { FaceId = faceId }
                    }
                ]
            });
        }

        public override Task<StartFaceSearchResponse> StartFaceSearchAsync(StartFaceSearchRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StartFaceSearchResponse { JobId = "face-search-job" });
        }

        public override Task<GetFaceSearchResponse> GetFaceSearchAsync(GetFaceSearchRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GetFaceSearchResponse
            {
                JobStatus = VideoJobStatus.SUCCEEDED,
                VideoMetadata = new VideoMetadata { DurationMillis = 100000 },
                Persons =
                [
                    new PersonMatch
                    {
                        Timestamp = 83200,
                        FaceMatches =
                        [
                            new FaceMatch
                            {
                                Face = new Face { FaceId = faceId },
                                Similarity = 94.7f
                            }
                        ]
                    }
                ]
            });
        }

        public override Task<DeleteFacesResponse> DeleteFacesAsync(DeleteFacesRequest request, CancellationToken cancellationToken = default)
        {
            if (ThrowOnDeleteFaces)
            {
                throw new AmazonRekognitionException("delete failed");
            }

            return Task.FromResult(new DeleteFacesResponse());
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
