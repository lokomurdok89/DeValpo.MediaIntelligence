using DeValpo.MediaIntelligence.Contracts.Auth;

namespace DeValpo.MediaIntelligence.Api.Auth;

public interface IClientAccessTokenService
{
    /// <summary>
    /// Validates a client API key and returns a short-lived JWT when valid.
    /// </summary>
    Task<AccessTokenResponse?> CreateTokenAsync(
        CreateAccessTokenRequest request,
        CancellationToken cancellationToken);
}
