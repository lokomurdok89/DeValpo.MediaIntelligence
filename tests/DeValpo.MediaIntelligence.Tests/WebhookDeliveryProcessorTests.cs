using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Domain.Entities;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Webhooks;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class WebhookDeliveryProcessorTests
{
    [Fact]
    public async Task ProcessNextDueAsync_WhenSenderSucceeds_MarksDeliveryDelivered()
    {
        var delivery = CreateDelivery();
        var repository = new FakeWebhookDeliveryRepository(delivery);
        var processor = new WebhookDeliveryProcessor(
            repository,
            new FakeWebhookDeliverySender(new WebhookSendResult(true, 204, null)),
            Options.Create(new WebhookDeliveryOptions { MaxAttempts = 3, InitialBackoffSeconds = 1 }));

        var processed = await processor.ProcessNextDueAsync(CancellationToken.None);

        processed.Should().BeTrue();
        delivery.Status.Should().Be("Delivered");
        delivery.AttemptCount.Should().Be(1);
        delivery.LastStatusCode.Should().Be(204);
        delivery.LastError.Should().BeNull();
        delivery.CompletedAt.Should().NotBeNull();
        delivery.NextAttemptAt.Should().BeNull();
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task ProcessNextDueAsync_WhenSenderFailsBeforeMaxAttempts_SchedulesRetry()
    {
        var delivery = CreateDelivery();
        var repository = new FakeWebhookDeliveryRepository(delivery);
        var processor = new WebhookDeliveryProcessor(
            repository,
            new FakeWebhookDeliverySender(new WebhookSendResult(false, null, "Connection refused")),
            Options.Create(new WebhookDeliveryOptions { MaxAttempts = 3, InitialBackoffSeconds = 1 }));

        var processed = await processor.ProcessNextDueAsync(CancellationToken.None);

        processed.Should().BeTrue();
        delivery.Status.Should().Be("RetryPending");
        delivery.AttemptCount.Should().Be(1);
        delivery.LastStatusCode.Should().BeNull();
        delivery.LastError.Should().Be("Connection refused");
        delivery.CompletedAt.Should().BeNull();
        delivery.NextAttemptAt.Should().NotBeNull();
        repository.SaveCount.Should().Be(1);
    }

    private static WebhookDelivery CreateDelivery()
    {
        return new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            JobId = Guid.NewGuid(),
            ClientKey = "generic",
            TargetKey = "qa-local",
            EventType = "analysis.completed",
            PayloadJson = "{\"eventType\":\"analysis.completed\"}",
            Status = "Pending",
            NextAttemptAt = DateTimeOffset.UtcNow,
            ClientWebhookTarget = new ClientWebhookTarget
            {
                Id = Guid.NewGuid(),
                TargetKey = "qa-local",
                Url = "https://example.test/webhook",
                SigningSecret = "qa-webhook-secret-min-32-chars"
            }
        };
    }

    private sealed class FakeWebhookDeliveryRepository(WebhookDelivery? delivery) : IWebhookDeliveryRepository
    {
        public int SaveCount { get; private set; }

        public Task AddRangeAsync(IReadOnlyCollection<WebhookDelivery> deliveries, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<WebhookDelivery>> ListAsync(
            string? clientKey,
            Guid? jobId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<WebhookDelivery>>([]);
        }

        public Task<WebhookDelivery?> GetNextDueAsync(int maxAttempts, CancellationToken cancellationToken)
        {
            return Task.FromResult(delivery);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWebhookDeliverySender(WebhookSendResult result) : IWebhookDeliverySender
    {
        public Task<WebhookSendResult> SendAsync(WebhookDelivery delivery, CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }
    }
}
