using DeValpo.MediaIntelligence.Api.Options;
using DeValpo.MediaIntelligence.Contracts.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DeValpo.MediaIntelligence.Api.Auth;

public sealed class DevJwtTokenService(IOptions<JwtOptions> options) : IDevJwtTokenService
{
    /// <summary>
    /// Builds a short-lived development token using the configured local signing key.
    /// </summary>
    public DevTokenResponse CreateToken(CreateDevTokenRequest request)
    {
        var config = options.Value;
        var expiresAt = DateTimeOffset.UtcNow.AddHours(8);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, DefaultIfEmpty(request.Subject, "local-dev-user")),
            new Claim("client_key", DefaultIfEmpty(request.ClientKey, "local-dev-client")),
            new Claim("scope", DefaultIfEmpty(request.Scope, "media-intelligence:mvp"))
        };

        var token = new JwtSecurityToken(
            issuer: config.Issuer,
            audience: config.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new DevTokenResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            expiresAt);
    }

    /// <summary>
    /// Returns a fallback claim value when the request omits one.
    /// </summary>
    private static string DefaultIfEmpty(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
