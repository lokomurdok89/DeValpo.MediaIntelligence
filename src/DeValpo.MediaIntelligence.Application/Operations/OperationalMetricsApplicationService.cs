using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Operations;

namespace DeValpo.MediaIntelligence.Application.Operations;

public sealed class OperationalMetricsApplicationService(IOperationalMetricsRepository repository)
    : IOperationalMetricsApplicationService
{
    /// <summary>
    /// Returns the current persisted operational metrics.
    /// </summary>
    public Task<OperationalMetricsResponse> GetCurrentAsync(CancellationToken cancellationToken)
    {
        return repository.GetCurrentAsync(cancellationToken);
    }
}
