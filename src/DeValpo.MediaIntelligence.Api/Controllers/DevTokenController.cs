using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/dev/token")]
public sealed class DevTokenController(
    IDevJwtTokenService tokenService,
    IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>
    /// Issues a local development JWT and stays unavailable outside Development.
    /// </summary>
    /// <remarks>
    /// Development-only helper used to test protected endpoints from Swagger UI or curl in local Docker.
    /// </remarks>
    /// <param name="request">Optional subject, client key and scope values for the local token.</param>
    /// <response code="200">Returns a signed local JWT.</response>
    /// <response code="404">The endpoint is unavailable outside Development.</response>
    [HttpPost]
    [ProducesResponseType(typeof(DevTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Create([FromBody] CreateDevTokenRequest? request)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(tokenService.CreateToken(request ?? new CreateDevTokenRequest(null, null, null)));
    }
}
