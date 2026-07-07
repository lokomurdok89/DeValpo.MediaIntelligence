using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/providers/aws-rekognition")]
public sealed class AwsRekognitionProviderController(
    IAwsRekognitionStatusService statusService,
    IRequestClientAccessService accessService) : ControllerBase
{
    /// <summary>
    /// Reports AWS Rekognition configuration status and optionally checks the S3 bucket and collection.
    /// </summary>
    /// <remarks>
    /// Basic status can be checked without a token. Remote AWS validation requires operator or admin access.
    /// </remarks>
    /// <param name="checkRemote">When true, validates that the configured S3 bucket and Rekognition collection are reachable.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns local configuration status and optional remote validation result.</response>
    /// <response code="401">Remote validation was requested without a valid bearer token.</response>
    /// <response code="403">Remote validation was requested without operator or admin scope.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(AwsRekognitionStatusResponse), StatusCodes.Status200OK)]
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
