namespace DeValpo.MediaIntelligence.Contracts.Auth;

public sealed record ApiClientWithKeyResponse(
    string ClientKey,
    string DisplayName,
    string? ContactEmail,
    IReadOnlyCollection<string> AllowedProfileKeys,
    Guid ApiKeyId,
    string KeyPrefix,
    string ApiKey,
    string Scope,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);
