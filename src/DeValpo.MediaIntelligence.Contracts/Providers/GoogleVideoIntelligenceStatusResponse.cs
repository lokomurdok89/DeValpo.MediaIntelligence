namespace DeValpo.MediaIntelligence.Contracts.Providers;

public sealed record GoogleVideoIntelligenceStatusResponse(
    bool Enabled,
    bool Configured,
    bool CredentialFilePresent,
    string BucketName,
    IReadOnlyCollection<string> MissingFields,
    string Message);
