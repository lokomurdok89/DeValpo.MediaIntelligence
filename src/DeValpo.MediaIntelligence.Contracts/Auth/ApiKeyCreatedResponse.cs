namespace DeValpo.MediaIntelligence.Contracts.Auth;

public sealed record ApiKeyCreatedResponse(
    Guid ApiKeyId,
    string ClientKey,
    string KeyPrefix,
    string ApiKey,
    string Scope,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);
