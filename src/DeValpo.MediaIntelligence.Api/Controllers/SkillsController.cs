using DeValpo.MediaIntelligence.Application.Skills;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Route("api/v1/skills")]
public sealed class SkillsController(ISkillApplicationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        return Ok(await service.ListAsync(cancellationToken));
    }
}
