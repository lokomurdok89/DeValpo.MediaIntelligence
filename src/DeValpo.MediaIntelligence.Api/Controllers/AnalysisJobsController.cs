using DeValpo.MediaIntelligence.Application.AnalysisJobs;
using DeValpo.MediaIntelligence.Application.MediaAnalysis;
using DeValpo.MediaIntelligence.Contracts.AnalysisJobs;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Route("api/v1/analysis-jobs")]
public sealed class AnalysisJobsController(
    IAnalysisJobApplicationService jobService,
    IMediaAnalysisApplicationService analysisService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAnalysisJobRequest request,
        CancellationToken cancellationToken)
    {
        var response = await jobService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { jobId = response.JobId }, response);
    }

    [HttpGet("{jobId:guid}")]
    public async Task<IActionResult> GetById(Guid jobId, CancellationToken cancellationToken)
    {
        var response = await jobService.GetByIdAsync(jobId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("{jobId:guid}/result")]
    public async Task<IActionResult> GetResult(Guid jobId, CancellationToken cancellationToken)
    {
        var response = await analysisService.GetResultAsync(jobId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{jobId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid jobId, CancellationToken cancellationToken)
    {
        await jobService.CancelAsync(jobId, cancellationToken);
        return NoContent();
    }
}
