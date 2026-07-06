using DeValpo.MediaIntelligence.Application.Profiles;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Route("api/v1/profiles")]
public sealed class ProfilesController(IProfileApplicationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        return Ok(await service.ListAsync(cancellationToken));
    }
}
