using DeValpo.MediaIntelligence.Contracts.Operations;

namespace DeValpo.MediaIntelligence.Application.Operations;

public interface IOperationalMetricsApplicationService
{
    /// <summary>
    /// Returns a point-in-time snapshot of operational counters.
    /// </summary>
    Task<OperationalMetricsResponse> GetCurrentAsync(CancellationToken cancellationToken);
}
