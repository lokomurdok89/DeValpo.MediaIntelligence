using DeValpo.MediaIntelligence.Api.Resources;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Api.Health;

public sealed class LocalStorageHealthCheck(IOptions<StorageOptions> options) : IHealthCheck
{
    /// <summary>
    /// Verifies that the configured local storage directory can be created and written.
    /// </summary>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var path = Path.GetFullPath(options.Value.LocalRootPath);
            Directory.CreateDirectory(path);
            return Task.FromResult(HealthCheckResult.Healthy(ApiResourceText.LocalStorageHealthy));
        }
        catch (Exception exception)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(ApiResourceText.LocalStorageUnhealthy, exception));
        }
    }
}
