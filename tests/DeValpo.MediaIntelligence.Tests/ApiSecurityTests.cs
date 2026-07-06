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
                        ["Jwt:RequireHttpsMetadata"] = "false"
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
