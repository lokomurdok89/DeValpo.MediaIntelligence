using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.MediaAnalysis;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisJobs;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Contracts.Webhooks;
using DeValpo.MediaIntelligence.Application.Webhooks;
using DeValpo.MediaIntelligence.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class MediaAnalysisApplicationServiceTests
{
    [Fact]
    public async Task EnqueueAsync_StoresAssetAndLeavesJobPending()
    {
        var jobRepository = new FakeAnalysisJobRepository();
        var service = CreateService(jobRepository);

        var response = await service.EnqueueAsync(
            CreateRequest(),
            new MemoryStream([1, 2, 3]),
            CancellationToken.None);

        response.Status.Should().Be(AnalysisJobStatus.Pending.ToString());
        response.ProcessingAttempts.Should().Be(0);
        jobRepository.Items.Should().ContainSingle();
        jobRepository.Items[0].Id.Should().Be(response.JobId);
        jobRepository.Items[0].MediaAssets.Should().ContainSingle();
    }

    [Fact]
    public async Task ProcessNextPendingAsync_CompletesQueuedJob()
    {
        var jobRepository = new FakeAnalysisJobRepository();
        var service = CreateService(jobRepository);
        var response = await service.EnqueueAsync(CreateRequest(), new MemoryStream([1, 2, 3]), CancellationToken.None);

        var processed = await service.ProcessNextPendingAsync(CancellationToken.None);

        processed.Should().BeTrue();
        var job = jobRepository.Items.Single(item => item.Id == response.JobId);
        job.Status.Should().Be(AnalysisJobStatus.Completed.ToString());
        job.ProcessingAttempts.Should().Be(1);
        job.CompletedAt.Should().NotBeNull();
        job.AnalysisResult.Should().NotBeNull();
        job.AnalysisResult!.ExtractedFields.Should().ContainSingle(field => field.Name == "text");
    }

    [Fact]
    public async Task ProcessNextPendingAsync_RetriesTransientFailureBeforeFailing()
    {
        var jobRepository = new FakeAnalysisJobRepository();
        var service = CreateService(
            jobRepository,
            new ThrowingOrchestrator(),
            new AnalysisProcessingOptions { MaxProcessingAttempts = 2 });
        await service.EnqueueAsync(CreateRequest(), new MemoryStream([1, 2, 3]), CancellationToken.None);

        var firstProcessed = await service.ProcessNextPendingAsync(CancellationToken.None);
        var secondProcessed = await service.ProcessNextPendingAsync(CancellationToken.None);

        firstProcessed.Should().BeTrue();
        secondProcessed.Should().BeTrue();
        var job = jobRepository.Items.Single();
        job.ProcessingAttempts.Should().Be(2);
        job.Status.Should().Be(AnalysisJobStatus.Failed.ToString());
        job.ErrorMessage.Should().Be("Provider failed.");
        job.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetRunningJobsAsync_ReturnsInterruptedJobsToPending()
    {
        var jobRepository = new FakeAnalysisJobRepository();
        jobRepository.Items.Add(new AnalysisJob
        {
            Status = AnalysisJobStatus.Running.ToString(),
            ErrorMessage = "interrupted"
        });
        var service = CreateService(jobRepository);

        var resetCount = await service.ResetRunningJobsAsync(CancellationToken.None);

        resetCount.Should().Be(1);
        jobRepository.Items[0].Status.Should().Be(AnalysisJobStatus.Pending.ToString());
        jobRepository.Items[0].ErrorMessage.Should().BeNull();
    }

    private static CreateAnalysisJobRequest CreateRequest()
    {
        return new CreateAnalysisJobRequest(
            "client-a",
            "generic-document-ocr-v1",
            MediaAssetType.Document,
            "document.pdf",
            "application/pdf",
            null);
    }

    private static MediaAnalysisApplicationService CreateService(
        FakeAnalysisJobRepository jobRepository,
        IMediaAnalysisOrchestrator? orchestrator = null,
        AnalysisProcessingOptions? options = null)
    {
        return new MediaAnalysisApplicationService(
            orchestrator ?? new SuccessfulOrchestrator(),
            new FakeMediaAssetStorage(),
            new FakeProfileResolver(),
            jobRepository,
            new FakeWebhookDeliveryApplicationService(),
            new FakeUnitOfWork(),
            Options.Create(options ?? new AnalysisProcessingOptions()),
            NullLogger<MediaAnalysisApplicationService>.Instance);
    }

    private sealed class SuccessfulOrchestrator : IMediaAnalysisOrchestrator
    {
        public Task<AnalysisOrchestrationResult> AnalyzeAsync(
            AnalysisOrchestrationRequest request,
            Stream content,
            CancellationToken cancellationToken)
        {
            var skillResult = new SkillExecutionResult(
                "TestSkill",
                0.95m,
                [new ExtractedFieldResponse("text", "ok", 0.95m, "TestSkill")],
                [],
                [],
                [],
                []);

            return Task.FromResult(new AnalysisOrchestrationResult(0.95m, false, [skillResult], []));
        }
    }

    private sealed class ThrowingOrchestrator : IMediaAnalysisOrchestrator
    {
        public Task<AnalysisOrchestrationResult> AnalyzeAsync(
            AnalysisOrchestrationRequest request,
            Stream content,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Provider failed.");
        }
    }

    private sealed class FakeProfileResolver : IAnalysisProfileResolver
    {
        private static readonly AnalysisProfileDefinition Profile = new(
            "generic-document-ocr-v1",
            "generic",
            [MediaAssetType.Document],
            ["TestSkill"],
            0.85m,
            true,
            "Test profile");

        public Task<AnalysisProfileDefinition?> ResolveAsync(string profileKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<AnalysisProfileDefinition?>(Profile);
        }

        public Task<IReadOnlyCollection<AnalysisProfileDefinition>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<AnalysisProfileDefinition>>([Profile]);
        }
    }

    private sealed class FakeAnalysisJobRepository : IAnalysisJobRepository
    {
        public List<AnalysisJob> Items { get; } = [];

        public Task AddAsync(AnalysisJob job, CancellationToken cancellationToken)
        {
            Items.Add(job);
            return Task.CompletedTask;
        }

        public Task<AnalysisJob?> GetNextPendingWithAssetAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Items
                .Where(job => job.Status == AnalysisJobStatus.Pending.ToString() && job.MediaAssets.Count > 0)
                .OrderBy(job => job.CreatedAt)
                .FirstOrDefault());
        }

        public Task<bool> MarkRunningAsync(Guid jobId, CancellationToken cancellationToken)
        {
            var job = Items.Single(item => item.Id == jobId);
            if (job.Status != AnalysisJobStatus.Pending.ToString())
            {
                return Task.FromResult(false);
            }

            job.Status = AnalysisJobStatus.Running.ToString();
            job.ErrorMessage = null;
            job.ProcessingAttempts++;
            return Task.FromResult(true);
        }

        public Task<AnalysisJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(job => job.Id == jobId));
        }

        public Task<int> ResetRunningAsync(CancellationToken cancellationToken)
        {
            var running = Items.Where(job => job.Status == AnalysisJobStatus.Running.ToString()).ToArray();
            foreach (var job in running)
            {
                job.Status = AnalysisJobStatus.Pending.ToString();
                job.ErrorMessage = null;
            }

            return Task.FromResult(running.Length);
        }

        public Task SetProcessingFailureAsync(
            Guid jobId,
            string status,
            string errorMessage,
            DateTimeOffset? completedAt,
            CancellationToken cancellationToken)
        {
            var job = Items.Single(item => item.Id == jobId);
            job.Status = status;
            job.ErrorMessage = errorMessage;
            job.CompletedAt = completedAt;
            return Task.CompletedTask;
        }

        public Task CompleteProcessingAsync(
            Guid jobId,
            string status,
            AnalysisResult result,
            IReadOnlyCollection<SkillExecution> skillExecutions,
            IReadOnlyCollection<HumanReviewTask> reviewTasks,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken)
        {
            var job = Items.Single(item => item.Id == jobId);
            job.Status = status;
            job.ErrorMessage = null;
            job.CompletedAt = completedAt;
            job.AnalysisResult = result;
            job.SkillExecutions.AddRange(skillExecutions);
            job.HumanReviewTasks.AddRange(reviewTasks);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMediaAssetStorage : IMediaAssetStorage
    {
        private readonly Dictionary<string, byte[]> files = new(StringComparer.OrdinalIgnoreCase);

        public async Task<StoredMediaAsset> SaveAsync(
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            using var memory = new MemoryStream();
            await content.CopyToAsync(memory, cancellationToken);
            var path = Guid.NewGuid().ToString("N");
            files[path] = memory.ToArray();
            return new StoredMediaAsset(path, fileName, contentType, files[path].Length);
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
        {
            Stream stream = new MemoryStream(files[storagePath]);
            return Task.FromResult(stream);
        }

        public Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(files.Remove(storagePath));
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeWebhookDeliveryApplicationService : IWebhookDeliveryApplicationService
    {
        public Task<WebhookTargetResponse> CreateTargetAsync(
            string clientKey,
            CreateWebhookTargetRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<WebhookTargetResponse>> ListTargetsAsync(
            string clientKey,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<WebhookDeliveryResponse>> ListDeliveriesAsync(
            string? clientKey,
            Guid? jobId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task EnqueueForJobAsync(Guid jobId, string eventType, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
