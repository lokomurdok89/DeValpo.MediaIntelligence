namespace DeValpo.MediaIntelligence.Infrastructure.Options;

public sealed class GoogleDocumentAiStatusCacheOptions
{
    public int RemoteStatusTtlSeconds { get; set; } = 60;
    public int RemoteStatusTimeoutSeconds { get; set; } = 10;
}
