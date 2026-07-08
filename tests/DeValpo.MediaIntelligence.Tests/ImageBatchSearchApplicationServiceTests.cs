using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.CloudAssets;
using DeValpo.MediaIntelligence.Application.ImageBatchSearch;
using DeValpo.MediaIntelligence.Application.ImageBatchSearch.Models;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class ImageBatchSearchApplicationServiceTests
{
    [Fact]
    public async Task EnqueueAsync_RejectsManifestWithPersonalData()
    {
        var service = CreateService();
        var command = new CreateImageBatchSearchJobCommand(
            "generic",
            "image-batch-search-v1",
            """{"targets":[{"targetKey":"target-1","queryType":"VisibleNumber","visibleNumber":"123","rut":"11.111.111-1"}]}""",
            [CreateUpload("source.jpg")],
            []);

        var act = () => service.EnqueueAsync(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*personal data*");
    }

    [Fact]
    public async Task EnqueueAsync_AcceptsMixedTargetsAndRequiresMatchingReferenceImageKey()
    {
        var repository = new FakeImageBatchSearchJobRepository();
        var service = CreateService(repository);

        await service.EnqueueAsync(
            new CreateImageBatchSearchJobCommand(
                "generic",
                "image-batch-search-v1",
                """{"targets":[{"targetKey":"bib-123","queryType":"VisibleNumber","visibleNumber":"123"},{"targetKey":"face-1","queryType":"FaceReference","referenceImageKey":"reference-1"}]}""",
                [CreateUpload("source.jpg")],
                [CreateUpload("face.jpg")]),
            CancellationToken.None);

        repository.Job.Should().NotBeNull();
        repository.Job!.Assets.Should().HaveCount(2);
        repository.Job.Assets.Should().Contain(asset => asset.AssetKey == "image-1" && asset.AssetRole == "SourceImage");
        repository.Job.Assets.Should().Contain(asset => asset.AssetKey == "reference-1" && asset.AssetRole == "ReferenceImage");
    }

    [Fact]
    public async Task EnqueueAsync_AcceptsCloudAssetSelectorAndPersistsSnapshot()
    {
        var repository = new FakeImageBatchSearchJobRepository();
        var resolver = new FakeAssetReferenceResolver
        {
            ResolvedAssets =
            [
                new(
                    "image-100",
                    CloudStorageProviders.S3,
                    "event-bucket",
                    "events/race/image-100.jpg",
                    "us-east-1",
                    null,
                    "image/jpeg",
                    1234,
                    "checksum",
                    "version-1",
                    null,
                    "image-100.jpg")
            ]
        };
        var service = CreateService(repository, assetReferenceResolver: resolver);

        await service.EnqueueAsync(
            new CreateImageBatchSearchJobCommand(
                "generic",
                "image-batch-search-v1",
                """{"targets":[{"targetKey":"target-1","queryType":"VisibleNumber","visibleNumber":"123"}]}""",
                [],
                [],
                new CloudAssetSelector(
                    CloudAssetSelectorModes.ExplicitAssets,
                    CloudStorageProviders.S3,
                    "event-bucket",
                    null,
                    "us-east-1",
                    null,
                    [])),
            CancellationToken.None);

        repository.Job.Should().NotBeNull();
        var asset = repository.Job!.Assets.Should().ContainSingle().Subject;
        asset.InputMode.Should().Be(CloudAssetInputModes.CloudReference);
        asset.StorageProvider.Should().Be(CloudStorageProviders.S3);
        asset.BucketName.Should().Be("event-bucket");
        asset.ObjectKey.Should().Be("events/race/image-100.jpg");
        asset.StoragePath.Should().Be("s3://event-bucket/events/race/image-100.jpg");
    }

    [Fact]
    public async Task ProcessNextPendingAsync_GroupsResultsByTargetKey()
    {
        var imageId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repository = new FakeImageBatchSearchJobRepository { Job = CreatePendingJob(imageId) };
        var textProvider = new FakeTextProvider
        {
            Result = new ImageBatchProviderResult(
                [
                    new ImageBatchProviderMatch(imageId, "target-1", ImageBatchSearchQueryTypes.VisibleNumber, "image-1", "race.jpg", 0.91m, "runner 123", "FakeText")
                ],
                [],
                new Dictionary<Guid, string>())
        };
        var service = CreateService(repository, textProvider: textProvider);

        var processed = await service.ProcessNextPendingAsync(CancellationToken.None);
        var result = await service.GetResultAsync(repository.Job!.Id, CancellationToken.None);

        processed.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Results.Should().ContainSingle();
        result.Results.Single().TargetKey.Should().Be("target-1");
        result.Results.Single().Matches.Should().ContainSingle();
        result.Results.Single().Matches.Single().ImageKey.Should().Be("image-1");
        result.Results.Single().Matches.Single().DetectedText.Should().Be("runner 123");
    }

    [Fact]
    public async Task ProcessNextPendingAsync_PersistsUnsupportedIssueFromProvider()
    {
        var repository = new FakeImageBatchSearchJobRepository { Job = CreatePendingFaceJob() };
        var faceProvider = new FakeFaceProvider
        {
            Result = new ImageBatchProviderResult(
                [],
                [new ImageBatchProviderIssue("UNSUPPORTED", "not supported", "Unsupported", "FakeFace", "face-1", ImageBatchSearchQueryTypes.FaceReference)],
                new Dictionary<Guid, string>())
        };
        var service = CreateService(repository, faceProvider: faceProvider);

        await service.ProcessNextPendingAsync(CancellationToken.None);

        repository.CompletedIssues.Should().ContainSingle();
        repository.CompletedIssues.Single().Severity.Should().Be("Unsupported");
        repository.CompletedIssues.Single().TargetKey.Should().Be("face-1");
    }

    private static ImageBatchSearchApplicationService CreateService(
        FakeImageBatchSearchJobRepository? repository = null,
        FakeTextProvider? textProvider = null,
        FakeFaceProvider? faceProvider = null,
        FakeAssetReferenceResolver? assetReferenceResolver = null)
    {
        return new ImageBatchSearchApplicationService(
            new FakeStorage(),
            new FakeProfileResolver(),
            repository ?? new FakeImageBatchSearchJobRepository(),
            textProvider is null ? [] : [textProvider],
            faceProvider is null ? [] : [faceProvider],
            assetReferenceResolver ?? new FakeAssetReferenceResolver(),
            new FakeUnitOfWork(),
            Options.Create(new ImageBatchSearchOptions { Enabled = true }),
            Options.Create(new CloudAssetOptions()),
            NullLogger<ImageBatchSearchApplicationService>.Instance);
    }

    private static ImageBatchSearchUpload CreateUpload(string fileName)
    {
        return new ImageBatchSearchUpload(fileName, "image/jpeg", new MemoryStream([0xff, 0xd8, 0xff, 0xd9]));
    }

    private static ImageBatchSearchJob CreatePendingJob(Guid imageId)
    {
        return new ImageBatchSearchJob
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ClientKey = "generic",
            ProfileKey = "image-batch-search-v1",
            Status = "Pending",
            ManifestJson = """{"targets":[{"targetKey":"target-1","queryType":"VisibleNumber","visibleNumber":"123"}]}""",
            Assets =
            [
                new()
                {
                    Id = imageId,
                    AssetKey = "image-1",
                    AssetRole = "SourceImage",
                    OriginalFileName = "race.jpg",
                    StoragePath = "stored/race.jpg",
                    ContentType = "image/jpeg",
                    SizeInBytes = 4
                }
            ]
        };
    }

    private static ImageBatchSearchJob CreatePendingFaceJob()
    {
        return new ImageBatchSearchJob
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            ClientKey = "generic",
            ProfileKey = "image-batch-search-v1",
            Status = "Pending",
            ManifestJson = """{"targets":[{"targetKey":"face-1","queryType":"FaceReference","referenceImageKey":"reference-1"}]}""",
            Assets =
            [
                new()
                {
                    Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    AssetKey = "image-1",
                    AssetRole = "SourceImage",
                    OriginalFileName = "race.jpg",
                    StoragePath = "stored/race.jpg",
                    ContentType = "image/jpeg",
                    SizeInBytes = 4
                },
                new()
                {
                    Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    AssetKey = "reference-1",
                    AssetRole = "ReferenceImage",
                    OriginalFileName = "face.jpg",
                    StoragePath = "stored/face.jpg",
                    ContentType = "image/jpeg",
                    SizeInBytes = 4
                }
            ]
        };
    }

    private sealed class FakeStorage : IMediaAssetStorage
    {
        public Task<StoredMediaAsset> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
        {
            return Task.FromResult(new StoredMediaAsset($"stored/{fileName}", fileName, contentType, content.Length));
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream>(new MemoryStream([0xff, 0xd8, 0xff, 0xd9]));
        }

        public Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FakeAssetReferenceResolver : IAssetReferenceResolver
    {
        public IReadOnlyCollection<ResolvedJobAsset> ResolvedAssets { get; init; } = [];

        public Task<IReadOnlyCollection<ResolvedJobAsset>> ResolveAsync(
            CloudAssetSelector selector,
            int maxAssets,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ResolvedAssets);
        }
    }

    private sealed class FakeProfileResolver : IAnalysisProfileResolver
    {
        public Task<AnalysisProfileDefinition?> ResolveAsync(string profileKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<AnalysisProfileDefinition?>(new(
                "image-batch-search-v1",
                "generic",
                [MediaAssetType.Image],
                [],
                0.80m,
                true,
                "Image batch search"));
        }

        public Task<IReadOnlyCollection<AnalysisProfileDefinition>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<AnalysisProfileDefinition>>([]);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTextProvider : IImageBatchTextSearchProvider
    {
        public string SourceProvider => "FakeText";
        public ImageBatchProviderResult Result { get; init; } = new([], [], new Dictionary<Guid, string>());

        public Task<ImageBatchProviderResult> SearchTextAsync(ImageBatchTextSearchRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeFaceProvider : IImageBatchFaceSearchProvider
    {
        public string SourceProvider => "FakeFace";
        public ImageBatchProviderResult Result { get; init; } = new([], [], new Dictionary<Guid, string>());

        public Task<ImageBatchProviderResult> SearchFacesAsync(ImageBatchFaceSearchRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeImageBatchSearchJobRepository : IImageBatchSearchJobRepository
    {
        public ImageBatchSearchJob? Job { get; set; }
        public IReadOnlyCollection<ImageBatchSearchMatch> CompletedMatches { get; private set; } = [];
        public IReadOnlyCollection<ImageBatchSearchIssue> CompletedIssues { get; private set; } = [];

        public Task AddAsync(ImageBatchSearchJob job, CancellationToken cancellationToken)
        {
            Job = job;
            return Task.CompletedTask;
        }

        public Task<ImageBatchSearchJob?> GetNextPendingWithAssetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Job?.Status == "Pending" ? Job : null);
        }

        public Task<bool> MarkRunningAsync(Guid jobId, CancellationToken cancellationToken)
        {
            if (Job is null || Job.Id != jobId || Job.Status != "Pending")
            {
                return Task.FromResult(false);
            }

            Job.Status = "Running";
            Job.ProcessingAttempts++;
            return Task.FromResult(true);
        }

        public Task<ImageBatchSearchJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Job?.Id == jobId ? Job : null);
        }

        public Task<int> ResetRunningAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task SetProcessingFailureAsync(Guid jobId, string status, string errorMessage, DateTimeOffset? completedAt, CancellationToken cancellationToken)
        {
            if (Job is not null)
            {
                Job.Status = status;
                Job.ErrorMessage = errorMessage;
                Job.CompletedAt = completedAt;
            }

            return Task.CompletedTask;
        }

        public Task CompleteProcessingAsync(
            Guid jobId,
            string status,
            IReadOnlyCollection<ImageBatchSearchMatch> matches,
            IReadOnlyCollection<ImageBatchSearchIssue> issues,
            IReadOnlyDictionary<Guid, string> providerInputUris,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken)
        {
            CompletedMatches = matches;
            CompletedIssues = issues;
            if (Job is not null)
            {
                Job.Status = status;
                foreach (var match in matches)
                {
                    Job.Matches.Add(match);
                }

                foreach (var issue in issues)
                {
                    Job.Issues.Add(issue);
                }

                Job.CompletedAt = completedAt;
            }

            return Task.CompletedTask;
        }
    }
}
