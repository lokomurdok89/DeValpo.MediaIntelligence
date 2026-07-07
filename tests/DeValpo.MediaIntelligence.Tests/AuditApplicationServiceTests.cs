using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Audit;
using DeValpo.MediaIntelligence.Domain.Entities;
using FluentAssertions;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class AuditApplicationServiceTests
{
    [Fact]
    public async Task RecordAsync_StoresSanitizedAuditMetadata()
    {
        var repository = new FakeAuditEventRepository();
        var service = new AuditApplicationService(repository, new FakeUnitOfWork());

        await service.RecordAsync(
            new AuditEventRequest(
                "api_key.rotated",
                "operator-1",
                "admin",
                "ApiKey",
                Guid.NewGuid().ToString(),
                "client-a",
                "Success",
                new Dictionary<string, string> { ["replacementApiKeyId"] = Guid.NewGuid().ToString() }),
            CancellationToken.None);

        repository.Items.Should().ContainSingle();
        repository.Items[0].MetadataJson.Should().Contain("replacementApiKeyId");
        repository.Items[0].MetadataJson.Should().NotContain("apiKey");
        repository.Items[0].MetadataJson.Should().NotContain("secret");
    }

    private sealed class FakeAuditEventRepository : IAuditEventRepository
    {
        public List<AuditEvent> Items { get; } = [];

        public Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Items.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<AuditEvent>> ListAsync(
            string? clientKey,
            string? eventType,
            int limit,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<AuditEvent>>(Items);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
