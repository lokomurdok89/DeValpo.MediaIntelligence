using DeValpo.MediaIntelligence.Application.Audit;
using DeValpo.MediaIntelligence.Contracts.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminScope")]
[Route("api/v1/admin/audit-events")]
public sealed class AdminAuditEventsController(IAuditApplicationService auditService) : ControllerBase
{
    /// <summary>
    /// Lists recent sanitized audit events.
    /// </summary>
    /// <remarks>
    /// Requires admin scope. Audit events intentionally exclude API keys, webhook secrets and document text.
    /// </remarks>
    /// <param name="clientKey">Optional client key filter.</param>
    /// <param name="eventType">Optional event type filter.</param>
    /// <param name="limit">Maximum number of events to return.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns recent audit events.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="403">The token does not include admin scope.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<AuditEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? clientKey,
        [FromQuery] string? eventType,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return Ok(await auditService.ListAsync(clientKey, eventType, limit, cancellationToken));
    }
}
