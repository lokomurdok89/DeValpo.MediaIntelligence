using DeValpo.MediaIntelligence.Application.Capabilities;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Route("api/v1/capabilities")]
public sealed class CapabilitiesController(ICapabilityApplicationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        return Ok(await service.ListAsync(cancellationToken));
    }
}
