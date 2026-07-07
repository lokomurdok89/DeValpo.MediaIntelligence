using DeValpo.MediaIntelligence.Api.Health;
using DeValpo.MediaIntelligence.Api.Options;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Providers;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class GoogleDocumentAiReadinessHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenGoogleDisabledAndNotRequired_ReturnsHealthy()
    {
        var check = new GoogleDocumentAiReadinessHealthCheck(
            new FakeGoogleDocumentAiStatusService(CreateStatus(enabled: false, configured: false)),
            Options.Create(new GoogleDocumentAiOptions { Enabled = false }),
            Options.Create(new ReadinessOptions { RequireGoogleDocumentAi = false }));

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenGoogleRequiredAndNotConfigured_ReturnsUnhealthy()
    {
        var check = new GoogleDocumentAiReadinessHealthCheck(
            new FakeGoogleDocumentAiStatusService(CreateStatus(enabled: true, configured: false)),
            Options.Create(new GoogleDocumentAiOptions { Enabled = true }),
            Options.Create(new ReadinessOptions { RequireGoogleDocumentAi = true }));

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data.Should().ContainKey("message");
    }

    private static GoogleDocumentAiStatusResponse CreateStatus(bool enabled, bool configured)
    {
        return new GoogleDocumentAiStatusResponse(
            enabled,
            configured,
            CredentialFilePresent: false,
            ProjectId: "project",
            Location: "us",
            ProcessorId: "processor",
            MissingFields: [],
            Message: configured ? "ready" : "not ready");
    }

    private sealed class FakeGoogleDocumentAiStatusService(GoogleDocumentAiStatusResponse response)
        : IGoogleDocumentAiStatusService
    {
        public Task<GoogleDocumentAiStatusResponse> GetStatusAsync(
            bool checkRemote,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(response);
        }
    }
}
