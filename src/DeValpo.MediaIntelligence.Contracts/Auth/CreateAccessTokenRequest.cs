namespace DeValpo.MediaIntelligence.Contracts.Auth;

public sealed record CreateAccessTokenRequest(
    string ClientKey,
    string ApiKey);
