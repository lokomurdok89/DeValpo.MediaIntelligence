using DeValpo.MediaIntelligence.Application.Capabilities;
using DeValpo.MediaIntelligence.Contracts.Capabilities;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Route("api/v1/capabilities")]
public sealed class CapabilitiesController(ICapabilityApplicationService service) : ControllerBase
{
    /// <summary>
    /// Lists the platform capabilities currently exposed by the API.
    /// </summary>
    /// <remarks>
    /// Returns capability metadata used by profiles and skills, such as OCR, document extraction and human review.
    /// </remarks>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns the available platform capabilities.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<CapabilityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        return Ok(await service.ListAsync(cancellationToken));
    }
}
