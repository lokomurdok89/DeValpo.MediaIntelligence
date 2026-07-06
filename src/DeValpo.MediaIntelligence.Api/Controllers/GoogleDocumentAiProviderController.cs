using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/providers/google-document-ai")]
public sealed class GoogleDocumentAiProviderController(
    IGoogleDocumentAiStatusService statusService) : ControllerBase
{
    /// <summary>
    /// Reports Google Document AI configuration status and optionally checks the remote processor.
    /// </summary>
    /// <remarks>
    /// Basic status can be checked without a token. Remote processor validation requires authentication because it touches Google Cloud.
    /// </remarks>
    /// <param name="checkRemote">When true, validates that the configured processor is reachable in Google Cloud.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns local configuration status and optional remote validation result.</response>
    /// <response code="401">Remote validation was requested without a valid bearer token.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(GoogleDocumentAiStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Status(
        [FromQuery] bool checkRemote = false,
        CancellationToken cancellationToken = default)
    {
        if (checkRemote && User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        return Ok(await statusService.GetStatusAsync(checkRemote, cancellationToken));
    }
}
