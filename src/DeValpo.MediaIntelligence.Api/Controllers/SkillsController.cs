using DeValpo.MediaIntelligence.Application.Skills;
using DeValpo.MediaIntelligence.Contracts.Skills;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Route("api/v1/skills")]
public sealed class SkillsController(ISkillApplicationService service) : ControllerBase
{
    /// <summary>
    /// Lists registered analysis skills and their supported media types.
    /// </summary>
    /// <remarks>
    /// Use this endpoint to inspect which skill implementations are currently registered in dependency injection.
    /// </remarks>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns registered skills with supported assets and required capabilities.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<SkillResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        return Ok(await service.ListAsync(cancellationToken));
    }
}
