namespace DeValpo.MediaIntelligence.Contracts.Auth;

public sealed record RotateApiKeyRequest(
    string? KeyName,
    string? Scope,
    DateTimeOffset? ExpiresAt);
