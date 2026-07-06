using DeValpo.MediaIntelligence.Contracts.Auth;

namespace DeValpo.MediaIntelligence.Api.Auth;

public interface IDevJwtTokenService
{
    /// <summary>
    /// Creates a signed JWT for local development API calls.
    /// </summary>
    DevTokenResponse CreateToken(CreateDevTokenRequest request);
}
