namespace DeValpo.MediaIntelligence.Contracts.Auth;

public sealed record CreateDevTokenRequest(
    string? Subject,
    string? ClientKey,
    string? Scope);
