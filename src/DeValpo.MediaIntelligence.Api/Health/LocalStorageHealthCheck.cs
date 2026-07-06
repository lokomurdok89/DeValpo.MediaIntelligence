using DeValpo.MediaIntelligence.Infrastructure.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Api.Health;

public sealed class LocalStorageHealthCheck(IOptions<StorageOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var path = Path.GetFullPath(options.Value.LocalRootPath);
            Directory.CreateDirectory(path);
            return Task.FromResult(HealthCheckResult.Healthy("Local storage is writable."));
        }
        catch (Exception exception)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Local storage is not writable.", exception));
        }
    }
}
