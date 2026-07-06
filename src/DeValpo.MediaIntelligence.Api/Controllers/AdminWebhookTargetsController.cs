using DeValpo.MediaIntelligence.Application.Webhooks;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.Webhooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminScope")]
[Route("api/v1/admin/clients/{clientKey}/webhook-targets")]
public sealed class AdminWebhookTargetsController(IWebhookDeliveryApplicationService service) : ControllerBase
{
    /// <summary>
    /// Registers a webhook target for a client.
    /// </summary>
    /// <remarks>
    /// Requires an admin token. The signing secret is stored for outbound HMAC signing and is never returned by the API.
    /// </remarks>
    /// <param name="clientKey">Client integration key.</param>
    /// <param name="request">Webhook target URL, target key and signing secret.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="201">Returns the created webhook target without the signing secret.</response>
    /// <response code="400">The target configuration is invalid or already exists.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="403">The token does not include an admin scope.</response>
    [HttpPost]
    [ProducesResponseType(typeof(WebhookTargetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        string clientKey,
        [FromBody] CreateWebhookTargetRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateTargetAsync(clientKey, request, cancellationToken);
        return Created($"/api/v1/admin/clients/{clientKey}/webhook-targets/{response.TargetId}", response);
    }

    /// <summary>
    /// Lists webhook targets configured for a client.
    /// </summary>
    /// <remarks>
    /// Returns target metadata without signing secrets.
    /// </remarks>
    /// <param name="clientKey">Client integration key.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns configured webhook targets.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="403">The token does not include an admin scope.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<WebhookTargetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(string clientKey, CancellationToken cancellationToken)
    {
        return Ok(await service.ListTargetsAsync(clientKey, cancellationToken));
    }
}
