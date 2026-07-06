using DeValpo.MediaIntelligence.Api.Options;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Auth;
using DeValpo.MediaIntelligence.Contracts.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DeValpo.MediaIntelligence.Api.Auth;

public sealed class ClientAccessTokenService(
    IClientApiKeyRepository apiKeyRepository,
    IApiKeyHasher keyHasher,
    IUnitOfWork unitOfWork,
    IOptions<JwtOptions> options) : IClientAccessTokenService
{
    /// <summary>
    /// Validates a hashed API key and issues a bearer token without exposing the key value.
    /// </summary>
    public async Task<AccessTokenResponse?> CreateTokenAsync(
        CreateAccessTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientKey) || string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return null;
        }

        var keyHash = keyHasher.Hash(request.ApiKey);
        var apiKey = await apiKeyRepository.GetByHashAsync(keyHash, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        if (apiKey?.Client is null ||
            !apiKey.Client.IsActive ||
            apiKey.RevokedAt is not null ||
            (apiKey.ExpiresAt is not null && apiKey.ExpiresAt <= now) ||
            !string.Equals(apiKey.Client.ClientKey, request.ClientKey.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        apiKey.LastUsedAt = now;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateJwt(apiKey.Client.ClientKey, apiKey.Scope, now);
    }

    private AccessTokenResponse CreateJwt(string clientKey, string scope, DateTimeOffset issuedAt)
    {
        var config = options.Value;
        var expiresAt = issuedAt.AddMinutes(config.AccessTokenMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var issuedAtUnix = issuedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, clientKey),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Iat, issuedAtUnix, ClaimValueTypes.Integer64),
            new Claim(AuthClaimTypes.ClientKey, clientKey),
            new Claim(AuthClaimTypes.Scope, scope)
        };

        var token = new JwtSecurityToken(
            issuer: config.Issuer,
            audience: config.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            expiresAt);
    }
}
