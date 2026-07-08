using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/providers/google-vision")]
public sealed class GoogleVisionProviderController(
    IGoogleVisionStatusService statusService,
    IRequestClientAccessService accessService) : ControllerBase
{
    /// <summary>
    /// Reports Google Vision configuration status and optionally checks credential usability.
    /// </summary>
    /// <remarks>
    /// Basic status can be checked without a token. Credential validation requires operator or admin access.
    /// </remarks>
    [HttpGet("status")]
    [ProducesResponseType(typeof(GoogleVisionStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Status(
        [FromQuery] bool checkRemote = false,
        CancellationToken cancellationToken = default)
    {
        if (checkRemote && User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        if (checkRemote && !accessService.HasOperationalAccess(User))
        {
            return Forbid();
        }

        return Ok(await statusService.GetStatusAsync(checkRemote, cancellationToken));
    }
}
