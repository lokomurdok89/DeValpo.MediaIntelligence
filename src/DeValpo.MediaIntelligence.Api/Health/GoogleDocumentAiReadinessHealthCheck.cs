using DeValpo.MediaIntelligence.Api.Options;
using DeValpo.MediaIntelligence.Api.Resources;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Api.Health;

public sealed class GoogleDocumentAiReadinessHealthCheck(
    IGoogleDocumentAiStatusService statusService,
    IOptions<GoogleDocumentAiOptions> googleOptions,
    IOptions<ReadinessOptions> readinessOptions) : IHealthCheck
{
    /// <summary>
    /// Checks whether the configured Google Document AI provider is ready for production traffic.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var google = googleOptions.Value;
        var readiness = readinessOptions.Value;

        if (!readiness.RequireGoogleDocumentAi && !google.Enabled)
        {
            return HealthCheckResult.Healthy(ApiResourceText.GoogleDocumentAiReadinessDisabled);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(readiness.TimeoutSeconds, 1)));

        var status = await statusService.GetStatusAsync(readiness.CheckGoogleDocumentAiRemote, timeoutCts.Token);
        var data = new Dictionary<string, object>
        {
            ["enabled"] = status.Enabled,
            ["configured"] = status.Configured,
            ["credentialFilePresent"] = status.CredentialFilePresent,
            ["location"] = status.Location,
            ["processorId"] = status.ProcessorId,
            ["missingFields"] = status.MissingFields
        };

        if (status.Configured)
        {
            return HealthCheckResult.Healthy(ApiResourceText.GoogleDocumentAiReadinessHealthy, data);
        }

        return HealthCheckResult.Unhealthy(
            ApiResourceText.GoogleDocumentAiReadinessUnhealthy,
            data: data.Append(new KeyValuePair<string, object>("message", status.Message))
                .ToDictionary(item => item.Key, item => item.Value));
    }
}
