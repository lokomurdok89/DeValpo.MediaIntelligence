namespace DeValpo.MediaIntelligence.Contracts.Providers;

public sealed record GoogleVisionStatusResponse(
    bool Enabled,
    bool Configured,
    bool CredentialSourcePresent,
    bool CredentialFilePresent,
    IReadOnlyCollection<string> MissingFields,
    string Message);
