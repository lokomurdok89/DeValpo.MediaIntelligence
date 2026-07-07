using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Operations;
using Microsoft.EntityFrameworkCore;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Repositories;

public sealed class OperationalMetricsRepository(MediaIntelligenceDbContext dbContext) : IOperationalMetricsRepository
{
    /// <summary>
    /// Aggregates persisted operational counters without returning document content.
    /// </summary>
    public async Task<OperationalMetricsResponse> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var jobsByStatus = await CountByStatusAsync(
            dbContext.AnalysisJobs.Select(job => job.Status),
            cancellationToken);
        var reviewTasksByStatus = await CountByStatusAsync(
            dbContext.HumanReviewTasks.Select(task => task.Status),
            cancellationToken);
        var webhookDeliveriesByStatus = await CountByStatusAsync(
            dbContext.WebhookDeliveries.Select(delivery => delivery.Status),
            cancellationToken);

        return new OperationalMetricsResponse(
            DateTimeOffset.UtcNow,
            jobsByStatus,
            reviewTasksByStatus,
            webhookDeliveriesByStatus,
            CountBucket(jobsByStatus, "Pending"),
            CountBucket(jobsByStatus, "Running"),
            CountBucket(reviewTasksByStatus, "Pending"),
            CountBucket(webhookDeliveriesByStatus, "Pending") + CountBucket(webhookDeliveriesByStatus, "RetryPending"));
    }

    private static async Task<IReadOnlyCollection<MetricBucketResponse>> CountByStatusAsync(
        IQueryable<string> statuses,
        CancellationToken cancellationToken)
    {
        var rows = await statuses
            .GroupBy(status => status)
            .Select(group => new { Name = group.Key, Count = group.Count() })
            .OrderBy(item => item.Name)
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(item => new MetricBucketResponse(item.Name, item.Count))
            .ToArray();
    }

    private static int CountBucket(IReadOnlyCollection<MetricBucketResponse> buckets, string name)
    {
        return buckets.FirstOrDefault(bucket => string.Equals(bucket.Name, name, StringComparison.OrdinalIgnoreCase))?.Count ?? 0;
    }
}
