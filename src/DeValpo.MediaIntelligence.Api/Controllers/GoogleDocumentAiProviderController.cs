using DeValpo.MediaIntelligence.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/providers/google-document-ai")]
public sealed class GoogleDocumentAiProviderController(
    IGoogleDocumentAiStatusService statusService) : ControllerBase
{
    [HttpGet("status")]
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
