using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Api.Audit;
using DeValpo.MediaIntelligence.Api.Resources;
using DeValpo.MediaIntelligence.Api.Validation;
using DeValpo.MediaIntelligence.Application.Audit;
using DeValpo.MediaIntelligence.Application.CloudAssets;
using DeValpo.MediaIntelligence.Application.ImageBatchSearch;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.ImageBatchSearch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Route("api/v1/image-batch-search-jobs")]
public sealed class ImageBatchSearchJobsController(
    IImageBatchSearchApplicationService service,
    IRequestClientAccessService accessService,
    IUploadedFileValidationService fileValidationService,
    IOptions<ImageBatchSearchOptions> options,
    IAuditApplicationService auditService) : ControllerBase
{
    private const long MultipartRequestSizeLimitBytes = 1_610_612_736;

    /// <summary>
    /// Creates an image batch search job for visible-number OCR and consented face-reference matching.
    /// </summary>
    /// <remarks>
    /// The manifest uses opaque targetKey values only. dataKey is a business synonym, not a contract field.
    /// </remarks>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MultipartRequestSizeLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MultipartRequestSizeLimitBytes)]
    [ProducesResponseType(typeof(CreateImageBatchSearchJobResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromForm] string clientKey,
        [FromForm] string profileKey,
        [FromForm] string manifestJson,
        [FromForm] List<IFormFile>? images,
        [FromForm] List<IFormFile>? referenceImages,
        [FromForm] string? assetSelectorJson,
        [FromForm] string? referenceAssetSelectorJson,
        CancellationToken cancellationToken)
    {
        if (!await accessService.CanSubmitAsync(User, clientKey, profileKey, cancellationToken))
        {
            await auditService.RecordAsync(
                AuditRequestFactory.Create(
                    User,
                    AuditEventTypes.ImageBatchSearchCreated,
                    "ImageBatchSearchJob",
                    null,
                    clientKey,
                    AuditOutcome.Denied,
                    new Dictionary<string, string> { ["profileKey"] = profileKey },
                    clientKey),
                cancellationToken);
            return Forbid();
        }

        var assetSelector = CloudAssetSelectorParser.ParseOptional(assetSelectorJson);
        var referenceAssetSelector = CloudAssetSelectorParser.ParseOptional(referenceAssetSelectorJson);
        var usesCloudImages = assetSelector is not null &&
            !string.Equals(assetSelector.SelectorMode, CloudAssetSelectorModes.MultipartUpload, StringComparison.OrdinalIgnoreCase);

        if (!usesCloudImages && (images is null || images.Count == 0))
        {
            return BadRequest(new ApiErrorResponse(
                FileValidationErrorCodes.FileRequired,
                ApiResourceText.ImageFilesRequired,
                HttpContext.TraceIdentifier));
        }

        if (!usesCloudImages && images!.Count > options.Value.MaxImagesPerJob)
        {
            return BadRequest(new ApiErrorResponse(
                FileValidationErrorCodes.FileTooLarge,
                ApiResourceText.ImageMaxFilesExceeded(options.Value.MaxImagesPerJob),
                HttpContext.TraceIdentifier));
        }

        var imageUploads = await ValidateUploadsAsync(images ?? [], cancellationToken);
        if (imageUploads.Error is not null)
        {
            return imageUploads.Error;
        }

        var referenceUploads = await ValidateUploadsAsync(referenceImages ?? [], cancellationToken);
        if (referenceUploads.Error is not null)
        {
            await DisposeUploadsAsync(imageUploads.Uploads);
            return referenceUploads.Error;
        }

        CreateImageBatchSearchJobResponse response;
        try
        {
            response = await service.EnqueueAsync(
                new CreateImageBatchSearchJobCommand(
                    clientKey,
                    profileKey,
                    manifestJson,
                    imageUploads.Uploads,
                    referenceUploads.Uploads,
                    assetSelector,
                    referenceAssetSelector),
                cancellationToken);
        }
        finally
        {
            await DisposeUploadsAsync(imageUploads.Uploads);
            await DisposeUploadsAsync(referenceUploads.Uploads);
        }

        await auditService.RecordAsync(
            AuditRequestFactory.Create(
                User,
                AuditEventTypes.ImageBatchSearchCreated,
                "ImageBatchSearchJob",
                response.JobId.ToString(),
                clientKey,
                AuditOutcome.Success,
                new Dictionary<string, string>
                {
                    ["profileKey"] = profileKey,
                    ["imageCount"] = (images?.Count ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["referenceImageCount"] = (referenceImages?.Count ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture)
                },
                clientKey),
            cancellationToken);

        return Accepted($"/api/v1/image-batch-search-jobs/{response.JobId}/result", response);
    }

    /// <summary>
    /// Returns image batch search job metadata and current processing status.
    /// </summary>
    [HttpGet("{jobId:guid}")]
    [ProducesResponseType(typeof(ImageBatchSearchJobResponse), StatusCodes.Status200OK)]
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
    /// Returns image matches grouped by targetKey for a completed or in-progress image batch search job.
    /// </summary>
    [HttpGet("{jobId:guid}/result")]
    [ProducesResponseType(typeof(ImageBatchSearchResultResponse), StatusCodes.Status200OK)]
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
                    AuditEventTypes.ImageBatchSearchResultRead,
                    "ImageBatchSearchJob",
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
                AuditEventTypes.ImageBatchSearchResultRead,
                "ImageBatchSearchJob",
                jobId.ToString(),
                response.ClientKey,
                AuditOutcome.Success,
                new Dictionary<string, string>
                {
                    ["status"] = response.Status,
                    ["targetCount"] = response.Results.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["matchCount"] = response.Results.Sum(result => result.Matches.Count).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["issueCount"] = response.Issues.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
                }),
            cancellationToken);

        return Ok(response);
    }

    private async Task<(IReadOnlyCollection<ImageBatchSearchUpload> Uploads, IActionResult? Error)> ValidateUploadsAsync(
        IReadOnlyCollection<IFormFile> files,
        CancellationToken cancellationToken)
    {
        var uploads = new List<ImageBatchSearchUpload>();
        foreach (var file in files)
        {
            var validation = await fileValidationService.ValidateAsync(
                file,
                MediaAssetType.Image,
                options.Value.MaxImageSizeBytes,
                cancellationToken);

            if (!validation.IsValid)
            {
                await DisposeUploadsAsync(uploads);
                return (uploads, BadRequest(new ApiErrorResponse(
                    validation.ErrorCode!,
                    validation.ErrorMessage!,
                    HttpContext.TraceIdentifier)));
            }

            uploads.Add(new ImageBatchSearchUpload(file.FileName, file.ContentType, file.OpenReadStream()));
        }

        return (uploads, null);
    }

    private static async Task DisposeUploadsAsync(IEnumerable<ImageBatchSearchUpload> uploads)
    {
        foreach (var upload in uploads)
        {
            await upload.Content.DisposeAsync();
        }
    }
}
