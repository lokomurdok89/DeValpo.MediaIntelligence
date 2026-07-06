using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Api.Options;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Auth;
using DeValpo.MediaIntelligence.Contracts.Auth;
using DeValpo.MediaIntelligence.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class ClientAccessTokenServiceTests
{
    [Fact]
    public async Task CreateTokenAsync_WithValidApiKey_ReturnsJwtWithClientClaims()
    {
        var hasher = new ApiKeyHasher();
        var rawKey = "dvmi_valid_raw_secret";
        var client = new Client { ClientKey = "client-a", DisplayName = "Client A" };
        var apiKey = new ClientApiKey
        {
            Client = client,
            ClientId = client.Id,
            KeyHash = hasher.Hash(rawKey),
            KeyPrefix = "dvmi_valid_raw_",
            Name = "primary",
            Scope = "media-intelligence:client"
        };
        var unitOfWork = new FakeUnitOfWork();
        var service = new ClientAccessTokenService(
            new FakeClientApiKeyRepository(apiKey),
            hasher,
            unitOfWork,
            Options.Create(CreateJwtOptions()));

        var response = await service.CreateTokenAsync(
            new CreateAccessTokenRequest("client-a", rawKey),
            CancellationToken.None);

        response.Should().NotBeNull();
        response!.TokenType.Should().Be("Bearer");
        apiKey.LastUsedAt.Should().NotBeNull();
        unitOfWork.SaveCount.Should().Be(1);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);
        token.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == "client-a");
        token.Claims.Should().Contain(claim => claim.Type == "client_key" && claim.Value == "client-a");
        token.Claims.Should().Contain(claim => claim.Type == "scope" && claim.Value == "media-intelligence:client");
        token.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Jti);
        token.Claims.Should().Contain(claim => claim.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public async Task CreateTokenAsync_WithRevokedApiKey_ReturnsNull()
    {
        var hasher = new ApiKeyHasher();
        var rawKey = "dvmi_revoked_raw_secret";
        var client = new Client { ClientKey = "client-a", DisplayName = "Client A" };
        var apiKey = new ClientApiKey
        {
            Client = client,
            ClientId = client.Id,
            KeyHash = hasher.Hash(rawKey),
            KeyPrefix = "dvmi_revoked_ra",
            Name = "primary",
            Scope = "media-intelligence:client",
            RevokedAt = DateTimeOffset.UtcNow
        };
        var unitOfWork = new FakeUnitOfWork();
        var service = new ClientAccessTokenService(
            new FakeClientApiKeyRepository(apiKey),
            hasher,
            unitOfWork,
            Options.Create(CreateJwtOptions()));

        var response = await service.CreateTokenAsync(
            new CreateAccessTokenRequest("client-a", rawKey),
            CancellationToken.None);

        response.Should().BeNull();
        unitOfWork.SaveCount.Should().Be(0);
    }

    private static JwtOptions CreateJwtOptions()
    {
        return new JwtOptions
        {
            Issuer = "DeValpo.MediaIntelligence",
            Audience = "DeValpo.MediaIntelligence.Api",
            SigningKey = "test-only-jwt-signing-key-with-32-chars",
            RequireHttpsMetadata = false,
            AccessTokenMinutes = 60
        };
    }

    private sealed class FakeClientApiKeyRepository(ClientApiKey apiKey) : IClientApiKeyRepository
    {
        public Task AddAsync(ClientApiKey apiKeyToAdd, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<ClientApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken)
        {
            return Task.FromResult(apiKey.KeyHash == keyHash ? apiKey : null);
        }

        public Task<ClientApiKey?> GetByIdAsync(Guid apiKeyId, CancellationToken cancellationToken)
        {
            return Task.FromResult(apiKey.Id == apiKeyId ? apiKey : null);
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
