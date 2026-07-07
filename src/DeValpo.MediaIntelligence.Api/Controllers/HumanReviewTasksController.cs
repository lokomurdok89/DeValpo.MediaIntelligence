using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Api.Audit;
using DeValpo.MediaIntelligence.Application.Audit;
using DeValpo.MediaIntelligence.Application.HumanReview;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.HumanReview;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Route("api/v1/human-review-tasks")]
public sealed class HumanReviewTasksController(
    IHumanReviewApplicationService service,
    IRequestClientAccessService accessService,
    IAuditApplicationService auditService) : ControllerBase
{
    private const string FallbackOperatorId = "unknown-operator";

    /// <summary>
    /// Lists human review tasks for operators.
    /// </summary>
    /// <remarks>
    /// Operators can filter by status to work pending, in-review, completed or rejected review tasks.
    /// </remarks>
    /// <param name="status">Optional review status filter.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns review tasks visible to operational users.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="403">The token does not have operator or admin scope.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<HumanReviewTaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        if (!accessService.HasOperationalAccess(User))
        {
            return Forbid();
        }

        return Ok(await service.ListAsync(status, cancellationToken));
    }

    /// <summary>
    /// Returns a human review task detail.
    /// </summary>
    /// <remarks>
    /// Includes current extracted fields and any corrected fields already submitted by an operator.
    /// </remarks>
    /// <param name="taskId">Human review task identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns review task detail.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="403">The token does not have operator or admin scope.</response>
    /// <response code="404">No review task exists with the provided identifier.</response>
    [HttpGet("{taskId:guid}")]
    [ProducesResponseType(typeof(HumanReviewTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid taskId, CancellationToken cancellationToken)
    {
        if (!accessService.HasOperationalAccess(User))
        {
            return Forbid();
        }

        var response = await service.GetByIdAsync(taskId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    /// <summary>
    /// Starts operator review for a pending task.
    /// </summary>
    /// <remarks>
    /// Marks the task as in review, stores the operator identity and optional opening comment.
    /// </remarks>
    /// <param name="taskId">Human review task identifier.</param>
    /// <param name="request">Optional start-review comment.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns the updated review task.</response>
    /// <response code="400">The task cannot be started from its current status.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="403">The token does not have operator or admin scope.</response>
    [HttpPost("{taskId:guid}/start")]
    [ProducesResponseType(typeof(HumanReviewTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Start(
        Guid taskId,
        [FromBody] StartHumanReviewTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!accessService.HasOperationalAccess(User))
        {
            return Forbid();
        }

        var response = await service.StartAsync(taskId, GetOperatorId(), request, cancellationToken);
        await auditService.RecordAsync(
            AuditRequestFactory.Create(
                User,
                AuditEventTypes.HumanReviewStarted,
                "HumanReviewTask",
                taskId.ToString(),
                response.ClientKey,
                AuditOutcome.Success),
            cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Completes a human review decision.
    /// </summary>
    /// <remarks>
    /// Supports Approve, Correct and Reject decisions. Correct preserves the original fields on the review task and writes corrected fields to the result.
    /// </remarks>
    /// <param name="taskId">Human review task identifier.</param>
    /// <param name="request">Review decision, comment and optional corrected fields.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns the completed review task.</response>
    /// <response code="400">The decision is invalid or the task cannot be completed.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="403">The token does not have operator or admin scope.</response>
    [HttpPost("{taskId:guid}/complete")]
    [ProducesResponseType(typeof(HumanReviewTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Complete(
        Guid taskId,
        [FromBody] CompleteHumanReviewTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!accessService.HasOperationalAccess(User))
        {
            return Forbid();
        }

        var response = await service.CompleteAsync(taskId, GetOperatorId(), request, cancellationToken);
        await auditService.RecordAsync(
            AuditRequestFactory.Create(
                User,
                AuditEventTypes.HumanReviewCompleted,
                "HumanReviewTask",
                taskId.ToString(),
                response.ClientKey,
                AuditOutcome.Success,
                new Dictionary<string, string> { ["decision"] = request.Decision.ToString() }),
            cancellationToken);
        return Ok(response);
    }

    private string GetOperatorId()
    {
        return User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.Identity?.Name ??
            FallbackOperatorId;
    }
}
