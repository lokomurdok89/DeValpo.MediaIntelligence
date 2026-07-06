using DeValpo.MediaIntelligence.Contracts.Auth;

namespace DeValpo.MediaIntelligence.Api.Auth;

public interface IDevJwtTokenService
{
    DevTokenResponse CreateToken(CreateDevTokenRequest request);
}
