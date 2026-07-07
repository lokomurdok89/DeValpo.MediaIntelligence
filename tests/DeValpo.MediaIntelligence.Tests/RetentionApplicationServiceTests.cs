using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Application.Retention;
using DeValpo.MediaIntelligence.Contracts.Retention;
using DeValpo.MediaIntelligence.Domain.Entities;
using FluentAssertions;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class RetentionApplicationServiceTests
{
    [Fact]
    public async Task UpsertPolicyAsync_WithInvalidRetentionDays_ThrowsClearError()
    {
        var service = new RetentionApplicationService(
            new FakeRetentionRepository(),
            new FakeMediaAssetStorage(),
            new FakeUnitOfWork());

        var act = () => service.UpsertPolicyAsync(
            "client-a",
            new RetentionPolicyRequest(true, 0),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Retention days must be greater than zero*");
    }

    [Fact]
    public async Task ExecuteAsync_WithEnabledPolicy_DeletesFilesAndMarksAssetsDeleted()
    {
        var asset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            StoragePath = "old.pdf",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-10)
        };
        var repository = new FakeRetentionRepository(asset)
        {
            Policy = new ClientRetentionPolicy
            {
                Client = new Client { ClientKey = "client-a" },
                Enabled = true,
                RetentionDays = 1
            }
        };
        var storage = new FakeMediaAssetStorage(["old.pdf"]);
        var service = new RetentionApplicationService(repository, storage, new FakeUnitOfWork());

        var response = await service.ExecuteAsync("client-a", CancellationToken.None);

        response.ScannedAssets.Should().Be(1);
        response.DeletedAssets.Should().Be(1);
        asset.DeletedAt.Should().NotBeNull();
        storage.DeletedPaths.Should().ContainSingle("old.pdf");
    }

    private sealed class FakeRetentionRepository(params MediaAsset[] assets) : IRetentionRepository
    {
        public ClientRetentionPolicy? Policy { get; set; }

        public Task<ClientRetentionPolicy?> GetPolicyAsync(string clientKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(Policy);
        }

        public Task<ClientRetentionPolicy> UpsertPolicyAsync(
            string clientKey,
            bool enabled,
            int retentionDays,
            CancellationToken cancellationToken)
        {
            Policy = new ClientRetentionPolicy
            {
                Client = new Client { ClientKey = clientKey },
                Enabled = enabled,
                RetentionDays = retentionDays,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            return Task.FromResult(Policy);
        }

        public Task<IReadOnlyCollection<MediaAsset>> ListExpiredAssetsAsync(
            string clientKey,
            DateTimeOffset cutoff,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<MediaAsset>>(assets);
        }
    }

    private sealed class FakeMediaAssetStorage(IEnumerable<string>? existingPaths = null) : IMediaAssetStorage
    {
        private readonly HashSet<string> existing = new(existingPaths ?? [], StringComparer.OrdinalIgnoreCase);

        public List<string> DeletedPaths { get; } = [];

        public Task<StoredMediaAsset> SaveAsync(
            Stream content,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken)
        {
            DeletedPaths.Add(storagePath);
            return Task.FromResult(existing.Remove(storagePath));
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
