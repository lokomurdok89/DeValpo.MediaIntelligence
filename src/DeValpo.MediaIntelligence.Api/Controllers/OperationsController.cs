using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Contracts.Operations;
using DeValpo.MediaIntelligence.Application.Operations;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Route("api/v1/operations")]
public sealed class OperationsController(
    IOperationalMetricsApplicationService metricsService,
    IRequestClientAccessService accessService) : ControllerBase
{
    /// <summary>
    /// Returns operational counters for queues, review tasks and webhook deliveries.
    /// </summary>
    /// <remarks>
    /// Requires operator or admin scope. The response contains counters only and never includes document text, API keys or webhook secrets.
    /// </remarks>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns current operational metrics.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="403">The token does not include operator or admin scope.</response>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(OperationalMetricsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Metrics(CancellationToken cancellationToken)
    {
        if (!accessService.HasOperationalAccess(User))
        {
            return Forbid();
        }

        return Ok(await metricsService.GetCurrentAsync(cancellationToken));
    }
}
