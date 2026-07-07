using DeValpo.MediaIntelligence.Contracts.Operations;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IOperationalMetricsRepository
{
    /// <summary>
    /// Reads current operational counters from persistence for support diagnostics.
    /// </summary>
    Task<OperationalMetricsResponse> GetCurrentAsync(CancellationToken cancellationToken);
}
