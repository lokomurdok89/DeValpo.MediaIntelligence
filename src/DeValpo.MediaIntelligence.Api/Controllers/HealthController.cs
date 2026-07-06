using DeValpo.MediaIntelligence.Api.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Returns a lightweight anonymous API health response.
    /// </summary>
    /// <remarks>
    /// Anonymous liveness endpoint for quick API checks. Use /health for full registered health checks.
    /// </remarks>
    /// <response code="200">Returns a healthy status and request trace identifier.</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new { status = ApiResourceText.HealthStatusHealthy, traceId = HttpContext.TraceIdentifier });
    }
}
