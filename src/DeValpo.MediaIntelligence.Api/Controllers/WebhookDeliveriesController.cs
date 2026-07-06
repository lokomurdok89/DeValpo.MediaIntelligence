using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Application.Webhooks;
using DeValpo.MediaIntelligence.Contracts.Webhooks;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Route("api/v1/webhook-deliveries")]
public sealed class WebhookDeliveriesController(
    IWebhookDeliveryApplicationService service,
    IRequestClientAccessService accessService) : ControllerBase
{
    /// <summary>
    /// Lists webhook delivery history.
    /// </summary>
    /// <remarks>
    /// Operators can inspect all deliveries. Client tokens must provide their own clientKey filter.
    /// </remarks>
    /// <param name="clientKey">Optional client key filter. Required for client-scoped tokens.</param>
    /// <param name="jobId">Optional analysis job filter.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns webhook delivery attempts and final status.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="403">The token cannot access the requested delivery history.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<WebhookDeliveryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? clientKey,
        [FromQuery] Guid? jobId,
        CancellationToken cancellationToken)
    {
        if (!accessService.HasOperationalAccess(User))
        {
            if (string.IsNullOrWhiteSpace(clientKey) ||
                !accessService.CanAccessClientResource(User, clientKey))
            {
                return Forbid();
            }
        }

        return Ok(await service.ListDeliveriesAsync(clientKey, jobId, cancellationToken));
    }
}
