using DeValpo.MediaIntelligence.Contracts.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class ApiSecurityTests
{
    [Fact]
    public async Task Capabilities_RequiresBearerToken()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/capabilities");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_RemainsAnonymous()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RootHealth_RemainsAnonymousAndLightweight()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Be("Healthy");
    }

    [Fact]
    public async Task OperationsMetrics_RequiresBearerToken()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/operations/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OperationsMetrics_RejectsClientOnlyToken()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var tokenResponse = await client.PostAsJsonAsync(
            "/api/v1/dev/token",
            new CreateDevTokenRequest("test-user", "test-client", "media-intelligence:client"));
        tokenResponse.EnsureSuccessStatusCode();

        var token = await tokenResponse.Content.ReadFromJsonAsync<DevTokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        var response = await client.GetAsync("/api/v1/operations/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("/api/v1/admin/audit-events")]
    [InlineData("/api/v1/admin/clients/test-client/retention-policy")]
    public async Task AdminEndpoints_RequireBearerToken(string path)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/v1/admin/audit-events")]
    [InlineData("/api/v1/admin/clients/test-client/retention-policy")]
    public async Task AdminEndpoints_RejectClientOnlyToken(string path)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var tokenResponse = await client.PostAsJsonAsync(
            "/api/v1/dev/token",
            new CreateDevTokenRequest("test-user", "test-client", "media-intelligence:client"));
        tokenResponse.EnsureSuccessStatusCode();

        var token = await tokenResponse.Content.ReadFromJsonAsync<DevTokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DevToken_AllowsCallingProtectedEndpoints_InDevelopment()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var tokenResponse = await client.PostAsJsonAsync(
            "/api/v1/dev/token",
            new CreateDevTokenRequest("test-user", "test-client", "test-scope"));
        tokenResponse.EnsureSuccessStatusCode();

        var token = await tokenResponse.Content.ReadFromJsonAsync<DevTokenResponse>();
        token.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        var capabilitiesResponse = await client.GetAsync("/api/v1/capabilities");

        capabilitiesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SwaggerJson_IncludesBearerSecurityScheme()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.Should().Contain("\"Bearer\"");
        json.Should().Contain("\"bearer\"");
    }

    [Fact]
    public async Task SwaggerJson_DoesNotDocumentRootRedirect()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.Should().NotContain("\"/\":");
    }

    [Fact]
    public async Task SwaggerJson_IncludesEndpointDescriptions()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        json.Should().Contain("Queues a document or image for background processing.");
        json.Should().Contain("Remote processor validation requires authentication");
        json.Should().Contain("Profiles define the supported asset types");
        json.Should().Contain("Lists recent sanitized audit events");
        json.Should().Contain("Creates or updates the retention policy for a client");
    }

    [Fact]
    public void Startup_Fails_WhenJwtSigningKeyIsMissing()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = string.Empty
        });

        var act = () => factory.CreateClient();

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*Jwt:SigningKey must be at least 32 bytes.*");
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IReadOnlyDictionary<string, string?>? overrides = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    var values = new Dictionary<string, string?>
                    {
                        ["Database:ApplyMigrationsOnStartup"] = "false",
                        ["Swagger:Enabled"] = "true",
                        ["GoogleDocumentAi:Enabled"] = "false",
                        ["Jwt:Issuer"] = "DeValpo.MediaIntelligence",
                        ["Jwt:Audience"] = "DeValpo.MediaIntelligence.Api",
                        ["Jwt:SigningKey"] = "test-only-jwt-signing-key-with-32-chars",
                        ["Jwt:RequireHttpsMetadata"] = "false",
                        ["AnalysisWorker:Enabled"] = "false"
                    };

                    if (overrides is not null)
                    {
                        foreach (var item in overrides)
                        {
                            values[item.Key] = item.Value;
                        }
                    }

                    configurationBuilder.AddInMemoryCollection(values);
                });
            });
    }
}
