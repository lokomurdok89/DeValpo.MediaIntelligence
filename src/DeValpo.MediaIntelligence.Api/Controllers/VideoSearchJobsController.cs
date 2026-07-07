using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Api.Resources;
using DeValpo.MediaIntelligence.Api.Validation;
using DeValpo.MediaIntelligence.Application.VideoSearch;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.VideoSearch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Route("api/v1/video-search-jobs")]
public sealed class VideoSearchJobsController(
    IVideoSearchApplicationService service,
    IRequestClientAccessService accessService,
    IUploadedFileValidationService fileValidationService,
    IOptions<VideoAnalysisOptions> videoOptions) : ControllerBase
{
    private const long MultipartRequestSizeLimitBytes = 1_610_612_736;

    /// <summary>
    /// Creates a video target search job from one or more source videos.
    /// </summary>
    /// <remarks>
    /// The manifest uses opaque targets and visible numbers only. Face detection returns generic face occurrences, not identity matches.
    /// </remarks>
    /// <param name="clientKey">Client integration key that owns the video search request.</param>
    /// <param name="profileKey">Must be event-video-target-search-v1.</param>
    /// <param name="manifestJson">JSON manifest with targets and detection options.</param>
    /// <param name="videos">One or more MP4/MOV/AVI source videos.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="202">Returns the queued video search job identifier.</response>
    /// <response code="400">The request is invalid or a video cannot be processed.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="403">The token cannot submit work for the requested client or profile.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MultipartRequestSizeLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MultipartRequestSizeLimitBytes)]
    [ProducesResponseType(typeof(CreateVideoSearchJobResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromForm] string clientKey,
        [FromForm] string profileKey,
        [FromForm] string manifestJson,
        [FromForm] List<IFormFile> videos,
        CancellationToken cancellationToken)
    {
        if (!await accessService.CanSubmitAsync(User, clientKey, profileKey, cancellationToken))
        {
            return Forbid();
        }

        if (videos is null || videos.Count == 0)
        {
            return BadRequest(new ApiErrorResponse(
                FileValidationErrorCodes.FileRequired,
                ApiResourceText.VideoFilesRequired,
                HttpContext.TraceIdentifier));
        }

        if (videos.Count > videoOptions.Value.MaxVideosPerJob)
        {
            return BadRequest(new ApiErrorResponse(
                FileValidationErrorCodes.FileTooLarge,
                ApiResourceText.VideoMaxFilesExceeded(videoOptions.Value.MaxVideosPerJob),
                HttpContext.TraceIdentifier));
        }

        var uploads = new List<VideoSearchUpload>();
        foreach (var video in videos)
        {
            var validation = await fileValidationService.ValidateAsync(
                video,
                MediaAssetType.Video,
                videoOptions.Value.MaxVideoSizeBytes,
                cancellationToken);

            if (!validation.IsValid)
            {
                return BadRequest(new ApiErrorResponse(
                    validation.ErrorCode!,
                    validation.ErrorMessage!,
                    HttpContext.TraceIdentifier));
            }

            uploads.Add(new VideoSearchUpload(video.FileName, video.ContentType, video.OpenReadStream()));
        }

        CreateVideoSearchJobResponse response;
        try
        {
            response = await service.EnqueueAsync(
                new CreateVideoSearchJobCommand(clientKey, profileKey, manifestJson, uploads),
                cancellationToken);
        }
        finally
        {
            foreach (var upload in uploads)
            {
                await upload.Content.DisposeAsync();
            }
        }

        return Accepted($"/api/v1/video-search-jobs/{response.JobId}/result", response);
    }

    /// <summary>
    /// Returns video search job metadata and current processing status.
    /// </summary>
    /// <param name="jobId">Video search job identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns the video search job status.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="404">The job was not found or is not owned by the authenticated client.</response>
    [HttpGet("{jobId:guid}")]
    [ProducesResponseType(typeof(VideoSearchJobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid jobId, CancellationToken cancellationToken)
    {
        var response = await service.GetByIdAsync(jobId, cancellationToken);
        if (response is null || !accessService.CanAccessClientResource(User, response.ClientKey))
        {
            return NotFound();
        }

        return Ok(response);
    }

    /// <summary>
    /// Returns target match and generic face occurrence timestamps for a video search job.
    /// </summary>
    /// <param name="jobId">Video search job identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns the video search result.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="404">The job was not found or is not owned by the authenticated client.</response>
    [HttpGet("{jobId:guid}/result")]
    [ProducesResponseType(typeof(VideoSearchResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResult(Guid jobId, CancellationToken cancellationToken)
    {
        var response = await service.GetResultAsync(jobId, cancellationToken);
        if (response is null || !accessService.CanAccessClientResource(User, response.ClientKey))
        {
            return NotFound();
        }

        return Ok(response);
    }
}
