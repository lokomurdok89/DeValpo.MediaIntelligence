using System.Security.Claims;
using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Domain.Entities;
using FluentAssertions;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class RequestClientAccessServiceTests
{
    [Fact]
    public async Task CanSubmitAsync_AllowsClient_WhenClientClaimAndProfileMatch()
    {
        var service = new RequestClientAccessService(new FakeClientRepository([
            new Client
            {
                ClientKey = "client-a",
                DisplayName = "Client A",
                AllowedProfileKeysJson = "[\"generic-document-ocr-v1\"]"
            }
        ]));

        var allowed = await service.CanSubmitAsync(
            CreatePrincipal("client-a", "media-intelligence:client"),
            "client-a",
            "generic-document-ocr-v1",
            CancellationToken.None);

        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task CanSubmitAsync_DeniesClient_WhenClientClaimDoesNotMatchRequest()
    {
        var service = new RequestClientAccessService(new FakeClientRepository([
            new Client
            {
                ClientKey = "client-a",
                DisplayName = "Client A",
                AllowedProfileKeysJson = "[\"generic-document-ocr-v1\"]"
            }
        ]));

        var allowed = await service.CanSubmitAsync(
            CreatePrincipal("client-b", "media-intelligence:client"),
            "client-a",
            "generic-document-ocr-v1",
            CancellationToken.None);

        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task CanSubmitAsync_DeniesClient_WhenProfileIsNotAssigned()
    {
        var service = new RequestClientAccessService(new FakeClientRepository([
            new Client
            {
                ClientKey = "client-a",
                DisplayName = "Client A",
                AllowedProfileKeysJson = "[\"other-profile-v1\"]"
            }
        ]));

        var allowed = await service.CanSubmitAsync(
            CreatePrincipal("client-a", "media-intelligence:client"),
            "client-a",
            "generic-document-ocr-v1",
            CancellationToken.None);

        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task CanSubmitAsync_AllowsAdminScope_ForOperationalWork()
    {
        var service = new RequestClientAccessService(new FakeClientRepository([]));

        var allowed = await service.CanSubmitAsync(
            CreatePrincipal("admin", "media-intelligence:admin"),
            "client-a",
            "generic-document-ocr-v1",
            CancellationToken.None);

        allowed.Should().BeTrue();
    }

    [Fact]
    public void CanAccessClientResource_DeniesCrossClientAccess()
    {
        var service = new RequestClientAccessService(new FakeClientRepository([]));

        var allowed = service.CanAccessClientResource(
            CreatePrincipal("client-b", "media-intelligence:client"),
            "client-a");

        allowed.Should().BeFalse();
    }

    [Fact]
    public void CanAccessClientResource_AllowsOperatorScope()
    {
        var service = new RequestClientAccessService(new FakeClientRepository([]));

        var allowed = service.CanAccessClientResource(
            CreatePrincipal("operator", "media-intelligence:operator"),
            "client-a");

        allowed.Should().BeTrue();
    }

    private static ClaimsPrincipal CreatePrincipal(string clientKey, string scope)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("client_key", clientKey),
                new Claim("scope", scope)
            ],
            "Test"));
    }

    private sealed class FakeClientRepository(IReadOnlyCollection<Client> clients) : IClientRepository
    {
        public Task AddAsync(Client client, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Client?> GetByKeyAsync(string clientKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(clients.FirstOrDefault(client =>
                string.Equals(client.ClientKey, clientKey, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
