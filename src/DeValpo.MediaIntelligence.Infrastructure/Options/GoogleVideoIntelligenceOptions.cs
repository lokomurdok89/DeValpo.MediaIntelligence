namespace DeValpo.MediaIntelligence.Infrastructure.Options;

public sealed class GoogleVideoIntelligenceOptions
{
    public bool Enabled { get; init; }
    public string CredentialsPath { get; init; } = string.Empty;
    public string BucketName { get; init; } = string.Empty;
    public bool AllowUserCredentials { get; init; }
}
