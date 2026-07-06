using System.Text.Json;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Webhooks;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.Webhooks;
using DeValpo.MediaIntelligence.Domain.Entities;
using FluentAssertions;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class WebhookDeliveryApplicationServiceTests
{
    [Fact]
    public async Task EnqueueForJobAsync_WithActiveTargets_CreatesMinimalSignedEventPayload()
    {
        var client = new Client { Id = Guid.NewGuid(), ClientKey = "generic", DisplayName = "Generic" };
        var target = new ClientWebhookTarget
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            Client = client,
            TargetKey = "qa-local",
            Url = "https://example.test/webhook",
            SigningSecret = "qa-webhook-secret-min-32-chars",
            IsActive = true
        };
        var job = new AnalysisJob
        {
            Id = Guid.NewGuid(),
            ClientKey = "generic",
            ProfileKey = "invoice-extraction-v1",
            AssetType = MediaAssetType.Document.ToString(),
            Status = AnalysisJobStatus.Completed.ToString(),
            AnalysisResult = new AnalysisResult
            {
                Id = Guid.NewGuid(),
                Confidence = 0.89m,
                RequiresHumanReview = false
            }
        };
        job.AnalysisResult.ExtractedFields.Add(new ExtractedField
        {
            Name = "text",
            Value = "DANIEL ELIAS TOLEDO MARTINEZ OCR TEXT SHOULD NOT BE SENT",
            Confidence = 0.89m,
            SourceSkill = "GoogleDocumentAiDocumentOcrSkill"
        });

        var deliveryRepository = new FakeWebhookDeliveryRepository();
        var service = new WebhookDeliveryApplicationService(
            new FakeClientRepository(client),
            new FakeClientWebhookTargetRepository([target]),
            deliveryRepository,
            new FakeAnalysisJobRepository(job),
            new FakeUnitOfWork());

        await service.EnqueueForJobAsync(job.Id, "analysis.completed", CancellationToken.None);

        deliveryRepository.AddedDeliveries.Should().ContainSingle();
        var delivery = deliveryRepository.AddedDeliveries.Single();
        delivery.ClientWebhookTargetId.Should().Be(target.Id);
        delivery.ClientKey.Should().Be("generic");
        delivery.TargetKey.Should().Be("qa-local");
        delivery.EventType.Should().Be("analysis.completed");
        delivery.Status.Should().Be("Pending");
        delivery.NextAttemptAt.Should().NotBeNull();
        delivery.PayloadJson.Should().NotContain(target.SigningSecret);
        delivery.PayloadJson.Should().NotContain("DANIEL ELIAS");

        using var document = JsonDocument.Parse(delivery.PayloadJson);
        var root = document.RootElement;
        root.GetProperty("eventId").GetGuid().Should().Be(delivery.Id);
        root.GetProperty("jobId").GetGuid().Should().Be(job.Id);
        root.GetProperty("clientKey").GetString().Should().Be("generic");
        root.GetProperty("profileKey").GetString().Should().Be("invoice-extraction-v1");
        root.GetProperty("resultUrl").GetString().Should().Be($"/api/v1/analysis-jobs/{job.Id}/result");
    }

    [Fact]
    public async Task CreateTargetAsync_DoesNotReturnSigningSecret()
    {
        var client = new Client { Id = Guid.NewGuid(), ClientKey = "generic", DisplayName = "Generic" };
        var service = new WebhookDeliveryApplicationService(
            new FakeClientRepository(client),
            new FakeClientWebhookTargetRepository([]),
            new FakeWebhookDeliveryRepository(),
            new FakeAnalysisJobRepository(null),
            new FakeUnitOfWork());

        var response = await service.CreateTargetAsync(
            "generic",
            new CreateWebhookTargetRequest(
                "qa-local",
                "https://example.test/webhook",
                "qa-webhook-secret-min-32-chars",
                true),
            CancellationToken.None);

        response.ClientKey.Should().Be("generic");
        response.TargetKey.Should().Be("qa-local");
        response.Url.Should().Be("https://example.test/webhook");
    }

    private sealed class FakeClientRepository(Client client) : IClientRepository
    {
        public Task AddAsync(Client clientToAdd, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Client?> GetByKeyAsync(string clientKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Equals(client.ClientKey, clientKey, StringComparison.OrdinalIgnoreCase) ? client : null);
        }
    }

    private sealed class FakeClientWebhookTargetRepository(IReadOnlyCollection<ClientWebhookTarget> targets) : IClientWebhookTargetRepository
    {
        public List<ClientWebhookTarget> AddedTargets { get; } = [];

        public Task AddAsync(ClientWebhookTarget target, CancellationToken cancellationToken)
        {
            AddedTargets.Add(target);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ClientWebhookTarget>> ListByClientKeyAsync(
            string clientKey,
            CancellationToken cancellationToken)
        {
            var result = targets
                .Concat(AddedTargets)
                .Where(target => string.Equals(target.Client?.ClientKey, clientKey, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<ClientWebhookTarget>>(result);
        }

        public Task<IReadOnlyCollection<ClientWebhookTarget>> ListActiveByClientKeyAsync(
            string clientKey,
            CancellationToken cancellationToken)
        {
            var result = targets
                .Where(target =>
                    target.IsActive &&
                    string.Equals(target.Client?.ClientKey, clientKey, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<ClientWebhookTarget>>(result);
        }

        public Task<ClientWebhookTarget?> GetByIdAsync(Guid targetId, CancellationToken cancellationToken)
        {
            return Task.FromResult(targets.Concat(AddedTargets).FirstOrDefault(target => target.Id == targetId));
        }

        public Task<bool> ExistsAsync(Guid clientId, string targetKey, CancellationToken cancellationToken)
        {
            var exists = targets
                .Concat(AddedTargets)
                .Any(target => target.ClientId == clientId &&
                    string.Equals(target.TargetKey, targetKey, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(exists);
        }
    }

    private sealed class FakeWebhookDeliveryRepository : IWebhookDeliveryRepository
    {
        public List<WebhookDelivery> AddedDeliveries { get; } = [];

        public Task AddRangeAsync(IReadOnlyCollection<WebhookDelivery> deliveries, CancellationToken cancellationToken)
        {
            AddedDeliveries.AddRange(deliveries);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<WebhookDelivery>> ListAsync(
            string? clientKey,
            Guid? jobId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<WebhookDelivery>>(AddedDeliveries);
        }

        public Task<WebhookDelivery?> GetNextDueAsync(int maxAttempts, CancellationToken cancellationToken)
        {
            return Task.FromResult<WebhookDelivery?>(null);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeAnalysisJobRepository(AnalysisJob? job) : IAnalysisJobRepository
    {
        public Task AddAsync(AnalysisJob jobToAdd, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<AnalysisJob?> GetNextPendingWithAssetAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<AnalysisJob?>(null);
        }

        public Task<bool> MarkRunningAsync(Guid jobId, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task<AnalysisJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
        {
            return Task.FromResult(job?.Id == jobId ? job : null);
        }

        public Task<int> ResetRunningAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public Task SetProcessingFailureAsync(
            Guid jobId,
            string status,
            string errorMessage,
            DateTimeOffset? completedAt,
            CancellationToken cancellationToken)
        {
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
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
