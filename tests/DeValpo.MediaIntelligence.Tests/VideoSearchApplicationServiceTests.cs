using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Application.VideoSearch;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class VideoSearchApplicationServiceTests
{
    [Fact]
    public async Task EnqueueAsync_RejectsManifestWithPersonalData()
    {
        var service = CreateService();
        var command = new CreateVideoSearchJobCommand(
            "generic",
            "event-video-target-search-v1",
            """{"targets":[{"targetKey":"target-1","visibleNumber":"123","email":"person@example.com"}]}""",
            [CreateUpload()]);

        var act = () => service.EnqueueAsync(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*personal data*");
    }

    [Fact]
    public async Task ProcessNextPendingAsync_GroupsVisibleNumberDetectionsIntoSegments()
    {
        var repository = new FakeVideoSearchJobRepository { Job = CreatePendingJob() };
        var provider = new FakeVideoSearchProvider
        {
            TextOccurrences =
            [
                new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "video-1", "race.mp4", "runner 123", 1m, 2m, 0.82m, "FakeProvider"),
                new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "video-1", "race.mp4", "bib 123", 3.5m, 4m, 0.91m, "FakeProvider")
            ]
        };
        var service = CreateService(repository, provider);

        var processed = await service.ProcessNextPendingAsync(CancellationToken.None);

        processed.Should().BeTrue();
        repository.CompletedTargetSegments.Should().ContainSingle();
        var segment = repository.CompletedTargetSegments.Single();
        segment.TargetKey.Should().Be("target-1");
        segment.VisibleNumber.Should().Be("123");
        segment.VideoKey.Should().Be("video-1");
        segment.StartTimeSeconds.Should().Be(1m);
        segment.EndTimeSeconds.Should().Be(4m);
        segment.Confidence.Should().Be(0.91m);
    }

    [Fact]
    public async Task ProcessNextPendingAsync_ReadsPersistedManifestWithSerializedPropertyNames()
    {
        var repository = new FakeVideoSearchJobRepository();
        var provider = new FakeVideoSearchProvider
        {
            TextOccurrences =
            [
                new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "video-1", "race.mp4", "runner 123", 1m, 2m, 0.82m, "FakeProvider")
            ]
        };
        var service = CreateService(repository, provider);

        await service.EnqueueAsync(
            new CreateVideoSearchJobCommand(
                "generic",
                "event-video-target-search-v1",
                """{"targets":[{"targetKey":"target-1","visibleNumber":"123"}],"mergeGapSeconds":2,"minConfidence":0.7}""",
                [CreateUpload()]),
            CancellationToken.None);

        var processed = await service.ProcessNextPendingAsync(CancellationToken.None);

        processed.Should().BeTrue();
        repository.CompletedTargetSegments.Should().ContainSingle();
        repository.Job!.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ProcessNextPendingAsync_PersistsGenericFaceOccurrencesWithoutTargetIdentity()
    {
        var repository = new FakeVideoSearchJobRepository { Job = CreatePendingJob() };
        var provider = new FakeVideoSearchProvider
        {
            FaceOccurrences =
            [
                new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "video-1", "race.mp4", 10m, 12m, 0.88m, "FakeProvider")
            ]
        };
        var service = CreateService(repository, provider);

        await service.ProcessNextPendingAsync(CancellationToken.None);

        repository.CompletedFaceSegments.Should().ContainSingle();
        var segment = repository.CompletedFaceSegments.Single();
        segment.VideoKey.Should().Be("video-1");
        segment.StartTimeSeconds.Should().Be(10m);
        segment.EndTimeSeconds.Should().Be(12m);
        segment.Confidence.Should().Be(0.88m);
    }

    private static VideoSearchApplicationService CreateService(
        FakeVideoSearchJobRepository? repository = null,
        FakeVideoSearchProvider? provider = null)
    {
        return new VideoSearchApplicationService(
            new FakeStorage(),
            new FakeProfileResolver(),
            repository ?? new FakeVideoSearchJobRepository(),
            provider ?? new FakeVideoSearchProvider(),
            new FakeUnitOfWork(),
            Options.Create(new VideoAnalysisOptions()),
            NullLogger<VideoSearchApplicationService>.Instance);
    }

    private static VideoSearchUpload CreateUpload()
    {
        return new VideoSearchUpload("race.mp4", "video/mp4", new MemoryStream([0x00, 0x00, 0x00, 0x18]));
    }

    private static VideoSearchJob CreatePendingJob()
    {
        return new VideoSearchJob
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ClientKey = "generic",
            ProfileKey = "event-video-target-search-v1",
            Status = "Pending",
            ManifestJson = """{"targets":[{"targetKey":"target-1","visibleNumber":"123"}],"mergeGapSeconds":2,"minConfidence":0.7}""",
            Assets =
            [
                new()
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    AssetKey = "video-1",
                    OriginalFileName = "race.mp4",
                    StoragePath = "race.mp4",
                    ContentType = "video/mp4",
                    SizeInBytes = 4
                }
            ]
        };
    }

    private sealed class FakeStorage : IMediaAssetStorage
    {
        public Task<StoredMediaAsset> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
        {
            return Task.FromResult(new StoredMediaAsset("stored/race.mp4", fileName, contentType, content.Length));
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

    private sealed class FakeProfileResolver : IAnalysisProfileResolver
    {
        public Task<AnalysisProfileDefinition?> ResolveAsync(string profileKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<AnalysisProfileDefinition?>(new(
                "event-video-target-search-v1",
                "generic",
                [MediaAssetType.Video],
                [],
                0.70m,
                true,
                "Video search"));
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

    private sealed class FakeVideoSearchProvider : IVideoSearchProvider
    {
        public IReadOnlyCollection<VideoTextOccurrence> TextOccurrences { get; init; } = [];
        public IReadOnlyCollection<VideoFaceOccurrence> FaceOccurrences { get; init; } = [];

        public Task<VideoSearchProviderResult> AnalyzeAsync(VideoSearchProviderRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new VideoSearchProviderResult(
                TextOccurrences,
                FaceOccurrences,
                [],
                new Dictionary<Guid, string>(),
                new Dictionary<Guid, decimal>()));
        }
    }

    private sealed class FakeVideoSearchJobRepository : IVideoSearchJobRepository
    {
        public VideoSearchJob? Job { get; set; }
        public IReadOnlyCollection<VideoSearchTargetMatchSegment> CompletedTargetSegments { get; private set; } = [];
        public IReadOnlyCollection<VideoSearchFaceOccurrenceSegment> CompletedFaceSegments { get; private set; } = [];

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
            IReadOnlyCollection<VideoSearchTargetMatchSegment> targetSegments,
            IReadOnlyCollection<VideoSearchFaceOccurrenceSegment> faceSegments,
            IReadOnlyCollection<VideoSearchIssue> issues,
            IReadOnlyDictionary<Guid, string> providerInputUris,
            IReadOnlyDictionary<Guid, decimal> videoDurations,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken)
        {
            CompletedTargetSegments = targetSegments;
            CompletedFaceSegments = faceSegments;
            if (Job is not null)
            {
                Job.Status = status;
                Job.TargetMatchSegments.AddRange(targetSegments);
                Job.FaceOccurrenceSegments.AddRange(faceSegments);
                Job.Issues.AddRange(issues);
                Job.CompletedAt = completedAt;
            }

            return Task.CompletedTask;
        }
    }
}
