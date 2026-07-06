using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Api.Resources;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/auth")]
public sealed class AuthController(IClientAccessTokenService tokenService) : ControllerBase
{
    /// <summary>
    /// Exchanges a client API key for a short-lived JWT.
    /// </summary>
    /// <remarks>
    /// Validates the API key hash, client status, key revocation and key expiration before issuing a bearer token.
    /// </remarks>
    /// <param name="request">Client key and raw API key. The raw key is never stored by the platform.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns a short-lived JWT.</response>
    /// <response code="401">Client credentials are invalid, revoked or expired.</response>
    [HttpPost("token")]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateToken(
        [FromBody] CreateAccessTokenRequest request,
        CancellationToken cancellationToken)
    {
        var token = await tokenService.CreateTokenAsync(request, cancellationToken);
        if (token is null)
        {
            return Unauthorized(new ApiErrorResponse(
                "INVALID_CLIENT_CREDENTIALS",
                ApiResourceText.InvalidClientCredentials,
                HttpContext.TraceIdentifier));
        }

        return Ok(token);
    }
}
