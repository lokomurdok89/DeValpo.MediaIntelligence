namespace DeValpo.MediaIntelligence.Infrastructure.Options;

public sealed class GoogleVisionOptions
{
    public bool Enabled { get; init; }
    public string CredentialsPath { get; init; } = string.Empty;
    public bool AllowUserCredentials { get; init; }
}
