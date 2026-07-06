using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace DeValpo.MediaIntelligence.Api.Health;

public sealed class PostgresHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = configuration.GetConnectionString("MediaIntelligence")
                ?? throw new InvalidOperationException("ConnectionStrings:MediaIntelligence is required.");

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("PostgreSQL is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is not reachable.", exception);
        }
    }
}
