using DeValpo.MediaIntelligence.Api.Audit;
using DeValpo.MediaIntelligence.Application.Auth;
using DeValpo.MediaIntelligence.Application.Audit;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminScope")]
[Route("api/v1/admin/clients")]
public sealed class AdminClientsController(
    IClientAccessApplicationService service,
    IAuditApplicationService auditService) : ControllerBase
{
    /// <summary>
    /// Creates an API client and returns its initial API key once.
    /// </summary>
    /// <remarks>
    /// Requires an admin-scoped bearer token. The returned API key must be copied by the operator because only its hash is stored.
    /// </remarks>
    /// <param name="request">Client metadata, optional allowed profiles and initial key metadata.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="201">Returns the created client plus the one-time raw API key.</response>
    /// <response code="400">The request is invalid or the client already exists.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="403">The token does not include an admin scope.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiClientWithKeyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateClient(
        [FromBody] CreateApiClientRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.CreateClientAsync(request, cancellationToken);
        await auditService.RecordAsync(
            AuditRequestFactory.Create(
                User,
                AuditEventTypes.ApiClientCreated,
                "Client",
                response.ClientKey,
                response.ClientKey,
                AuditOutcome.Success,
                new Dictionary<string, string> { ["apiKeyId"] = response.ApiKeyId.ToString() }),
            cancellationToken);
        return Created($"/api/v1/admin/clients/{response.ClientKey}", response);
    }

    /// <summary>
    /// Rotates a client API key and returns the replacement key once.
    /// </summary>
    /// <remarks>
    /// Revokes the existing key before returning the new raw key. Only the replacement key hash is persisted.
    /// </remarks>
    /// <param name="clientKey">Client integration key.</param>
    /// <param name="apiKeyId">API key identifier to revoke and replace.</param>
    /// <param name="request">Replacement key metadata.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns the one-time replacement API key.</response>
    /// <response code="400">The key cannot be rotated.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="403">The token does not include an admin scope.</response>
    [HttpPost("{clientKey}/api-keys/{apiKeyId:guid}/rotate")]
    [ProducesResponseType(typeof(ApiKeyCreatedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RotateApiKey(
        string clientKey,
        Guid apiKeyId,
        [FromBody] RotateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        var response = await service.RotateApiKeyAsync(clientKey, apiKeyId, request, cancellationToken);
        await auditService.RecordAsync(
            AuditRequestFactory.Create(
                User,
                AuditEventTypes.ApiKeyRotated,
                "ApiKey",
                apiKeyId.ToString(),
                clientKey,
                AuditOutcome.Success,
                new Dictionary<string, string> { ["replacementApiKeyId"] = response.ApiKeyId.ToString() }),
            cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Revokes a client API key.
    /// </summary>
    /// <remarks>
    /// Revoked keys can no longer mint JWTs. The hash remains for audit and duplicate detection.
    /// </remarks>
    /// <param name="clientKey">Client integration key.</param>
    /// <param name="apiKeyId">API key identifier to revoke.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="204">The key was revoked.</response>
    /// <response code="400">The key cannot be revoked.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="403">The token does not include an admin scope.</response>
    [HttpPost("{clientKey}/api-keys/{apiKeyId:guid}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RevokeApiKey(
        string clientKey,
        Guid apiKeyId,
        CancellationToken cancellationToken)
    {
        await service.RevokeApiKeyAsync(clientKey, apiKeyId, cancellationToken);
        await auditService.RecordAsync(
            AuditRequestFactory.Create(
                User,
                AuditEventTypes.ApiKeyRevoked,
                "ApiKey",
                apiKeyId.ToString(),
                clientKey,
                AuditOutcome.Success),
            cancellationToken);
        return NoContent();
    }
}
