using DeValpo.MediaIntelligence.Application.Profiles;
using DeValpo.MediaIntelligence.Contracts.Profiles;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Route("api/v1/profiles")]
public sealed class ProfilesController(IProfileApplicationService service) : ControllerBase
{
    /// <summary>
    /// Lists analysis profiles available to authenticated clients.
    /// </summary>
    /// <remarks>
    /// Profiles define the supported asset types, ordered skills, confidence threshold and review behavior for analysis.
    /// </remarks>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns the configured analysis profiles.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<AnalysisProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        return Ok(await service.ListAsync(cancellationToken));
    }
}
