using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Auth;
using DeValpo.MediaIntelligence.Contracts.Auth;
using DeValpo.MediaIntelligence.Domain.Entities;
using FluentAssertions;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class ClientAccessApplicationServiceTests
{
    [Fact]
    public async Task CreateClientAsync_StoresOnlyApiKeyHash()
    {
        var clientRepository = new FakeClientRepository();
        var apiKeyRepository = new FakeClientApiKeyRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new ClientAccessApplicationService(
            clientRepository,
            apiKeyRepository,
            new FixedApiKeySecretGenerator("dvmi_test_raw_secret"),
            new ApiKeyHasher(),
            unitOfWork);

        var response = await service.CreateClientAsync(
            new CreateApiClientRequest(
                "client-a",
                "Client A",
                "ops@example.com",
                ["generic-document-ocr-v1"],
                "primary",
                "media-intelligence:client",
                null),
            CancellationToken.None);

        response.ApiKey.Should().Be("dvmi_test_raw_secret");
        apiKeyRepository.Items.Should().ContainSingle();
        apiKeyRepository.Items[0].KeyHash.Should().NotBe(response.ApiKey);
        apiKeyRepository.Items[0].KeyHash.Should().Be(new ApiKeyHasher().Hash(response.ApiKey));
        apiKeyRepository.Items[0].KeyPrefix.Should().Be("dvmi_test_raw_se");
        unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task RotateApiKeyAsync_RevokesExistingKeyAndReturnsReplacementOnce()
    {
        var hasher = new ApiKeyHasher();
        var client = new Client { ClientKey = "client-a", DisplayName = "Client A" };
        var existingKey = new ClientApiKey
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            Client = client,
            Name = "primary",
            KeyPrefix = "old-prefix",
            KeyHash = hasher.Hash("old-secret"),
            Scope = "media-intelligence:client"
        };
        var apiKeyRepository = new FakeClientApiKeyRepository();
        apiKeyRepository.Items.Add(existingKey);
        var service = new ClientAccessApplicationService(
            new FakeClientRepository(),
            apiKeyRepository,
            new FixedApiKeySecretGenerator("dvmi_new_raw_secret"),
            hasher,
            new FakeUnitOfWork());

        var response = await service.RotateApiKeyAsync(
            "client-a",
            existingKey.Id,
            new RotateApiKeyRequest("replacement", null, null),
            CancellationToken.None);

        existingKey.RevokedAt.Should().NotBeNull();
        response.ApiKey.Should().Be("dvmi_new_raw_secret");
        apiKeyRepository.Items.Should().HaveCount(2);
        apiKeyRepository.Items[1].RotatedFromKeyId.Should().Be(existingKey.Id);
        apiKeyRepository.Items[1].KeyHash.Should().Be(hasher.Hash(response.ApiKey));
        apiKeyRepository.Items[1].KeyHash.Should().NotBe(response.ApiKey);
    }

    private sealed class FixedApiKeySecretGenerator(string secret) : IApiKeySecretGenerator
    {
        public string Generate() => secret;
    }

    private sealed class FakeClientRepository : IClientRepository
    {
        public List<Client> Items { get; } = [];

        public Task AddAsync(Client client, CancellationToken cancellationToken)
        {
            Items.Add(client);
            return Task.CompletedTask;
        }

        public Task<Client?> GetByKeyAsync(string clientKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(item => item.ClientKey == clientKey));
        }
    }

    private sealed class FakeClientApiKeyRepository : IClientApiKeyRepository
    {
        public List<ClientApiKey> Items { get; } = [];

        public Task AddAsync(ClientApiKey apiKey, CancellationToken cancellationToken)
        {
            Items.Add(apiKey);
            return Task.CompletedTask;
        }

        public Task<ClientApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(item => item.KeyHash == keyHash));
        }

        public Task<ClientApiKey?> GetByIdAsync(Guid apiKeyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(item => item.Id == apiKeyId));
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
