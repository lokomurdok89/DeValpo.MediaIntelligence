using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.CloudAssets;
using DeValpo.MediaIntelligence.Application.FaceSearch;
using DeValpo.MediaIntelligence.Application.FaceSearch.Models;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Application.VideoSearch;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class FaceSearchApplicationServiceTests
{
    [Fact]
    public async Task EnqueueAsync_RejectsManifestWithPersonalData()
    {
        var service = CreateService();
        var command = new CreateFaceSearchJobCommand(
            "generic",
            "event-face-search-v1",
            "consent-001",
            """{"targets":[{"targetKey":"target-1","referenceImageKey":"reference-1","email":"person@example.com"}]}""",
            [CreateUpload("race.mp4", "video/mp4")],
            [CreateUpload("face.jpg", "image/jpeg")]);

        var act = () => service.EnqueueAsync(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*personal data*");
    }

    [Fact]
    public async Task EnqueueAsync_StoresConsentAndReferenceAssets()
    {
        var repository = new FakeFaceSearchJobRepository();
        var service = CreateService(repository);

        await service.EnqueueAsync(
            new CreateFaceSearchJobCommand(
                "generic",
                "event-face-search-v1",
                "consent-001",
                """{"targets":[{"targetKey":"target-1","referenceImageKey":"reference-1"}],"minSimilarity":92}""",
                [CreateUpload("race.mp4", "video/mp4")],
                [CreateUpload("face.jpg", "image/jpeg")]),
            CancellationToken.None);

        repository.Job.Should().NotBeNull();
        repository.Job!.ConsentReferenceId.Should().Be("consent-001");
        repository.Job.Assets.Should().ContainSingle(item => item.AssetRole == "SourceVideo" && item.AssetKey == "video-1");
        repository.Job.Assets.Should().ContainSingle(item => item.AssetRole == "ReferenceImage" && item.AssetKey == "reference-1");
    }

    [Fact]
    public async Task EnqueueAsync_AllowsMissingConsentReference()
    {
        var repository = new FakeFaceSearchJobRepository();
        var service = CreateService(repository);

        await service.EnqueueAsync(
            new CreateFaceSearchJobCommand(
                "generic",
                "event-face-search-v1",
                null,
                """{"targets":[{"targetKey":"target-1","referenceImageKey":"reference-1"}]}""",
                [CreateUpload("race.mp4", "video/mp4")],
                [CreateUpload("face.jpg", "image/jpeg")]),
            CancellationToken.None);

        repository.Job.Should().NotBeNull();
        repository.Job!.ConsentReferenceId.Should().BeNull();
    }

    [Fact]
    public async Task ProcessNextPendingAsync_PersistsAwsFaceReferenceMatches()
    {
        var repository = new FakeFaceSearchJobRepository { Job = CreatePendingJob() };
        var provider = new FakeFaceSearchProvider
        {
            FaceMatches =
            [
                new(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "target-1",
                    "reference-1",
                    "video-1",
                    "race.mp4",
                    83.2m,
                    83.2m,
                    94.7m,
                    "AwsRekognition")
            ]
        };
        var service = CreateService(repository, provider);

        await service.ProcessNextPendingAsync(CancellationToken.None);

        repository.CompletedFaceMatches.Should().ContainSingle();
        var match = repository.CompletedFaceMatches.Single();
        match.TargetKey.Should().Be("target-1");
        match.ReferenceImageKey.Should().Be("reference-1");
        match.VideoKey.Should().Be("video-1");
        match.Similarity.Should().Be(94.7m);
        match.SourceProvider.Should().Be("AwsRekognition");
    }

    private static FaceSearchApplicationService CreateService(
        FakeFaceSearchJobRepository? repository = null,
        FakeFaceSearchProvider? provider = null)
    {
        return new FaceSearchApplicationService(
            new FakeStorage(),
            new FakeProfileResolver(),
            repository ?? new FakeFaceSearchJobRepository(),
            provider ?? new FakeFaceSearchProvider(),
            new FakeAssetReferenceResolver(),
            new FakeUnitOfWork(),
            Options.Create(new VideoAnalysisOptions()),
            Options.Create(new FaceSearchOptions()),
            Options.Create(new CloudAssetOptions()),
            NullLogger<FaceSearchApplicationService>.Instance);
    }

    private static VideoSearchUpload CreateUpload(string fileName, string contentType)
    {
        return new VideoSearchUpload(fileName, contentType, new MemoryStream([0x00, 0x00, 0x00, 0x18]));
    }

    private static VideoSearchJob CreatePendingJob()
    {
        return new VideoSearchJob
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ClientKey = "generic",
            ProfileKey = "event-face-search-v1",
            Status = "Pending",
            ConsentReferenceId = "consent-001",
            ManifestJson = """{"targets":[{"targetKey":"target-1","referenceImageKey":"reference-1"}],"minSimilarity":92}""",
            Assets =
            [
                new()
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    AssetKey = "video-1",
                    AssetRole = "SourceVideo",
                    OriginalFileName = "race.mp4",
                    StoragePath = "race.mp4",
                    ContentType = "video/mp4",
                    SizeInBytes = 4
                },
                new()
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    AssetKey = "reference-1",
                    AssetRole = "ReferenceImage",
                    OriginalFileName = "face.jpg",
                    StoragePath = "face.jpg",
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
            return Task.FromResult<Stream>(new MemoryStream([0x00, 0x00, 0x00, 0x18]));
        }

        public Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FakeAssetReferenceResolver : IAssetReferenceResolver
    {
        public Task<IReadOnlyCollection<ResolvedJobAsset>> ResolveAsync(
            CloudAssetSelector selector,
            int maxAssets,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<ResolvedJobAsset>>([]);
        }
    }

    private sealed class FakeProfileResolver : IAnalysisProfileResolver
    {
        public Task<AnalysisProfileDefinition?> ResolveAsync(string profileKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<AnalysisProfileDefinition?>(new(
                "event-face-search-v1",
                "generic",
                [MediaAssetType.Video, MediaAssetType.Image],
                [],
                0.90m,
                true,
                "Face search"));
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

    private sealed class FakeFaceSearchProvider : IFaceSearchProvider
    {
        public IReadOnlyCollection<FaceReferenceMatchOccurrence> FaceMatches { get; init; } = [];

        public Task<FaceSearchProviderResult> AnalyzeAsync(FaceSearchProviderRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new FaceSearchProviderResult(
                FaceMatches,
                [],
                new Dictionary<Guid, string>(),
                new Dictionary<Guid, decimal>()));
        }
    }

    private sealed class FakeFaceSearchJobRepository : IFaceSearchJobRepository
    {
        public VideoSearchJob? Job { get; set; }
        public IReadOnlyCollection<VideoSearchFaceReferenceMatchSegment> CompletedFaceMatches { get; private set; } = [];

        public Task AddAsync(VideoSearchJob job, CancellationToken cancellationToken)
        {
            Job = job;
            return Task.CompletedTask;
        }

        public Task<VideoSearchJob?> GetNextPendingWithAssetsAsync(CancellationToken cancellationToken)
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

        public Task<VideoSearchJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
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
            IReadOnlyCollection<VideoSearchFaceReferenceMatchSegment> faceMatches,
            IReadOnlyCollection<VideoSearchIssue> issues,
            IReadOnlyDictionary<Guid, string> providerInputUris,
            IReadOnlyDictionary<Guid, decimal> videoDurations,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken)
        {
            CompletedFaceMatches = faceMatches;
            if (Job is not null)
            {
                Job.Status = status;
                Job.FaceReferenceMatchSegments.AddRange(faceMatches);
                Job.Issues.AddRange(issues);
                Job.CompletedAt = completedAt;
            }

            return Task.CompletedTask;
        }
    }
}
