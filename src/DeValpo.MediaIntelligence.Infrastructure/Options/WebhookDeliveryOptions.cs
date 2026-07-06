namespace DeValpo.MediaIntelligence.Infrastructure.Options;

public sealed class WebhookDeliveryOptions
{
    public bool Enabled { get; init; } = true;
    public int PollIntervalSeconds { get; init; } = 2;
    public int MaxAttempts { get; init; } = 3;
    public int InitialBackoffSeconds { get; init; } = 5;
    public int RequestTimeoutSeconds { get; init; } = 10;
}
