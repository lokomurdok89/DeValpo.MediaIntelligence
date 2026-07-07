namespace DeValpo.MediaIntelligence.Contracts.Operations;

public sealed record OperationalMetricsResponse(
    DateTimeOffset GeneratedAt,
    IReadOnlyCollection<MetricBucketResponse> JobsByStatus,
    IReadOnlyCollection<MetricBucketResponse> HumanReviewTasksByStatus,
    IReadOnlyCollection<MetricBucketResponse> WebhookDeliveriesByStatus,
    int PendingJobs,
    int RunningJobs,
    int PendingHumanReviewTasks,
    int PendingWebhookDeliveries);
