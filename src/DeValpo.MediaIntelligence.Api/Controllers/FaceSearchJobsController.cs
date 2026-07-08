using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Api.Audit;
using DeValpo.MediaIntelligence.Api.Options;
using DeValpo.MediaIntelligence.Api.Resources;
using DeValpo.MediaIntelligence.Api.Validation;
using DeValpo.MediaIntelligence.Application.Audit;
using DeValpo.MediaIntelligence.Application.FaceSearch;
using DeValpo.MediaIntelligence.Application.VideoSearch;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.FaceSearch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Route("api/v1/face-search-jobs")]
public sealed class FaceSearchJobsController(
    IFaceSearchApplicationService service,
    IRequestClientAccessService accessService,
    IUploadedFileValidationService fileValidationService,
    IOptions<VideoAnalysisOptions> videoOptions,
    IOptions<FaceSearchOptions> faceOptions,
    IOptions<UploadValidationOptions> uploadOptions,
    IAuditApplicationService auditService) : ControllerBase
{
    private const long MultipartRequestSizeLimitBytes = 1_610_612_736;

    /// <summary>
    /// Creates an AWS Rekognition face search job from source videos and consented reference images.
    /// </summary>
    /// <remarks>
    /// The manifest uses opaque target keys only. Personal data belongs outside MediaIntelligence.
    /// </remarks>
    /// <param name="clientKey">Client integration key that owns the face search request.</param>
    /// <param name="profileKey">Must be event-face-search-v1.</param>
    /// <param name="consentReferenceId">External consent reference for the face search operation.</param>
    /// <param name="manifestJson">JSON manifest with opaque targets and reference image keys.</param>
    /// <param name="videos">One or more MP4/MOV/AVI source videos.</param>
    /// <param name="referenceImages">One or more reference images.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="202">Returns the queued face search job identifier.</response>
    /// <response code="400">The request is invalid or an uploaded file cannot be processed.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="403">The token cannot submit work for the requested client or profile.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MultipartRequestSizeLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MultipartRequestSizeLimitBytes)]
    [ProducesResponseType(typeof(CreateFaceSearchJobResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromForm] string clientKey,
        [FromForm] string profileKey,
        [FromForm] string consentReferenceId,
        [FromForm] string manifestJson,
        [FromForm] List<IFormFile> videos,
        [FromForm] List<IFormFile> referenceImages,
        CancellationToken cancellationToken)
    {
        if (!await accessService.CanSubmitAsync(User, clientKey, profileKey, cancellationToken))
        {
            await auditService.RecordAsync(
                AuditRequestFactory.Create(
                    User,
                    AuditEventTypes.FaceSearchCreated,
                    "VideoSearchJob",
                    null,
                    clientKey,
                    AuditOutcome.Denied,
                    new Dictionary<string, string>
                    {
                        ["profileKey"] = profileKey,
                        ["consentReferenceProvided"] = (!string.IsNullOrWhiteSpace(consentReferenceId)).ToString()
                    },
                    clientKey),
                cancellationToken);
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

        if (referenceImages is null || referenceImages.Count == 0)
        {
            return BadRequest(new ApiErrorResponse(
                FileValidationErrorCodes.FileRequired,
                ApiResourceText.ReferenceImagesRequired,
                HttpContext.TraceIdentifier));
        }

        if (referenceImages.Count > faceOptions.Value.MaxReferenceImagesPerJob)
        {
            return BadRequest(new ApiErrorResponse(
                FileValidationErrorCodes.FileTooLarge,
                ApiResourceText.ReferenceImageMaxFilesExceeded(faceOptions.Value.MaxReferenceImagesPerJob),
                HttpContext.TraceIdentifier));
        }

        var videoUploads = await ValidateUploadsAsync(
            videos,
            MediaAssetType.Video,
            videoOptions.Value.MaxVideoSizeBytes,
            cancellationToken);
        if (videoUploads.Error is not null)
        {
            return videoUploads.Error;
        }

        var referenceUploads = await ValidateUploadsAsync(
            referenceImages,
            MediaAssetType.Image,
            uploadOptions.Value.MaxFileSizeBytes,
            cancellationToken);
        if (referenceUploads.Error is not null)
        {
            await DisposeUploadsAsync(videoUploads.Uploads);
            return referenceUploads.Error;
        }

        CreateFaceSearchJobResponse response;
        try
        {
            response = await service.EnqueueAsync(
                new CreateFaceSearchJobCommand(
                    clientKey,
                    profileKey,
                    consentReferenceId,
                    manifestJson,
                    videoUploads.Uploads,
                    referenceUploads.Uploads),
                cancellationToken);
        }
        finally
        {
            await DisposeUploadsAsync(videoUploads.Uploads);
            await DisposeUploadsAsync(referenceUploads.Uploads);
        }

        await auditService.RecordAsync(
            AuditRequestFactory.Create(
                User,
                AuditEventTypes.FaceSearchCreated,
                "VideoSearchJob",
                response.JobId.ToString(),
                clientKey,
                AuditOutcome.Success,
                new Dictionary<string, string>
                {
                    ["profileKey"] = profileKey,
                    ["videoCount"] = videos.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["referenceImageCount"] = referenceImages.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["consentReferenceProvided"] = (!string.IsNullOrWhiteSpace(consentReferenceId)).ToString()
                },
                clientKey),
            cancellationToken);

        return Accepted($"/api/v1/face-search-jobs/{response.JobId}/result", response);
    }

    /// <summary>
    /// Returns face search job metadata and current processing status.
    /// </summary>
    [HttpGet("{jobId:guid}")]
    [ProducesResponseType(typeof(FaceSearchJobResponse), StatusCodes.Status200OK)]
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
    /// Returns AWS Rekognition face reference match timestamps for a face search job.
    /// </summary>
    [HttpGet("{jobId:guid}/result")]
    [ProducesResponseType(typeof(FaceSearchResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResult(Guid jobId, CancellationToken cancellationToken)
    {
        var response = await service.GetResultAsync(jobId, cancellationToken);
        if (response is null)
        {
            return NotFound();
        }

        if (!accessService.CanAccessClientResource(User, response.ClientKey))
        {
            await auditService.RecordAsync(
                AuditRequestFactory.Create(
                    User,
                    AuditEventTypes.FaceSearchResultRead,
                    "VideoSearchJob",
                    jobId.ToString(),
                    response.ClientKey,
                    AuditOutcome.Denied,
                    new Dictionary<string, string> { ["status"] = response.Status }),
                cancellationToken);
            return NotFound();
        }

        await auditService.RecordAsync(
            AuditRequestFactory.Create(
                User,
                AuditEventTypes.FaceSearchResultRead,
                "VideoSearchJob",
                jobId.ToString(),
                response.ClientKey,
                AuditOutcome.Success,
                new Dictionary<string, string>
                {
                    ["status"] = response.Status,
                    ["matchCount"] = response.FaceMatches.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["issueCount"] = response.Issues.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
                }),
            cancellationToken);

        return Ok(response);
    }

    private async Task<(IReadOnlyCollection<VideoSearchUpload> Uploads, IActionResult? Error)> ValidateUploadsAsync(
        IReadOnlyCollection<IFormFile> files,
        MediaAssetType assetType,
        long maxFileSizeBytes,
        CancellationToken cancellationToken)
    {
        var uploads = new List<VideoSearchUpload>();
        foreach (var file in files)
        {
            var validation = await fileValidationService.ValidateAsync(
                file,
                assetType,
                maxFileSizeBytes,
                cancellationToken);

            if (!validation.IsValid)
            {
                await DisposeUploadsAsync(uploads);
                return (uploads, BadRequest(new ApiErrorResponse(
                    validation.ErrorCode!,
                    validation.ErrorMessage!,
                    HttpContext.TraceIdentifier)));
            }

            uploads.Add(new VideoSearchUpload(file.FileName, file.ContentType, file.OpenReadStream()));
        }

        return (uploads, null);
    }

    private static async Task DisposeUploadsAsync(IEnumerable<VideoSearchUpload> uploads)
    {
        foreach (var upload in uploads)
        {
            await upload.Content.DisposeAsync();
        }
    }
}
