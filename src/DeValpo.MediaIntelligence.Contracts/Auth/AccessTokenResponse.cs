namespace DeValpo.MediaIntelligence.Contracts.Auth;

public sealed record AccessTokenResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt);
