using DeValpo.MediaIntelligence.Api.Audit;
using DeValpo.MediaIntelligence.Application.Audit;
using DeValpo.MediaIntelligence.Application.Retention;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.Retention;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminScope")]
[Route("api/v1/admin/clients/{clientKey}/retention-policy")]
public sealed class AdminRetentionController(
    IRetentionApplicationService retentionService,
    IAuditApplicationService auditService) : ControllerBase
{
    /// <summary>
    /// Returns the retention policy configured for a client.
    /// </summary>
    /// <param name="clientKey">Client integration key.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns the retention policy.</response>
    /// <response code="404">No retention policy exists for the client.</response>
    [HttpGet]
    [ProducesResponseType(typeof(RetentionPolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string clientKey, CancellationToken cancellationToken)
    {
        var response = await retentionService.GetPolicyAsync(clientKey, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    /// <summary>
    /// Creates or updates the retention policy for a client.
    /// </summary>
    /// <remarks>
    /// Requires admin scope. The policy controls future execution of media asset deletion.
    /// </remarks>
    /// <param name="clientKey">Client integration key.</param>
    /// <param name="request">Retention policy settings.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns the updated retention policy.</response>
    /// <response code="400">The policy request is invalid.</response>
    [HttpPut]
    [ProducesResponseType(typeof(RetentionPolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert(
        string clientKey,
        [FromBody] RetentionPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var response = await retentionService.UpsertPolicyAsync(clientKey, request, cancellationToken);
        await auditService.RecordAsync(
            AuditRequestFactory.Create(
                User,
                AuditEventTypes.RetentionPolicyUpdated,
                "Client",
                clientKey,
                clientKey,
                AuditOutcome.Success,
                new Dictionary<string, string>
                {
                    ["enabled"] = response.Enabled.ToString(),
                    ["retentionDays"] = response.RetentionDays.ToString(System.Globalization.CultureInfo.InvariantCulture)
                }),
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Executes the configured retention policy for a client.
    /// </summary>
    /// <remarks>
    /// Deletes eligible stored media files and marks their asset rows as deleted.
    /// </remarks>
    /// <param name="clientKey">Client integration key.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns retention execution counters.</response>
    /// <response code="400">The policy is missing or disabled.</response>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(RetentionExecutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Execute(string clientKey, CancellationToken cancellationToken)
    {
        var response = await retentionService.ExecuteAsync(clientKey, cancellationToken);
        await auditService.RecordAsync(
            AuditRequestFactory.Create(
                User,
                AuditEventTypes.RetentionExecuted,
                "Client",
                clientKey,
                clientKey,
                AuditOutcome.Success,
                new Dictionary<string, string>
                {
                    ["scannedAssets"] = response.ScannedAssets.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["deletedAssets"] = response.DeletedAssets.ToString(System.Globalization.CultureInfo.InvariantCulture)
                }),
            cancellationToken);

        return Ok(response);
    }
}
