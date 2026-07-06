namespace DeValpo.MediaIntelligence.Contracts.Auth;

public sealed record CreateApiClientRequest(
    string ClientKey,
    string DisplayName,
    string? ContactEmail,
    IReadOnlyCollection<string>? AllowedProfileKeys,
    string? KeyName,
    string? Scope,
    DateTimeOffset? ExpiresAt);
