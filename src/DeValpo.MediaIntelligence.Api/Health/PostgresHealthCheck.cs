using DeValpo.MediaIntelligence.Api.Resources;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace DeValpo.MediaIntelligence.Api.Health;

public sealed class PostgresHealthCheck(IConfiguration configuration) : IHealthCheck
{
    /// <summary>
    /// Opens a PostgreSQL connection and executes a lightweight readiness query.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = configuration.GetConnectionString("MediaIntelligence")
                ?? throw new InvalidOperationException(ApiResourceText.ConnectionStringRequired);

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy(ApiResourceText.PostgresHealthy);
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(ApiResourceText.PostgresUnhealthy, exception);
        }
    }
}
