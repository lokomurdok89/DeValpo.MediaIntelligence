using System.Text.Json;
using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Api.Resources;
using DeValpo.MediaIntelligence.Application.MediaAnalysis;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisJobs;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Route("api/v1/analyze")]
public sealed class AnalyzeController(
    IMediaAnalysisApplicationService service,
    IRequestClientAccessService accessService,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Accepts a media file, stores it and runs the configured analysis profile.
    /// </summary>
    /// <remarks>
    /// Runs a synchronous MVP analysis against an uploaded document or image. Use this for real end-to-end document processing with a valid JWT.
    /// </remarks>
    /// <param name="clientKey">Client integration key that owns the analysis request.</param>
    /// <param name="profileKey">Analysis profile key that selects the skill pipeline to execute.</param>
    /// <param name="assetType">Type of media being uploaded.</param>
    /// <param name="file">Binary media file to analyze.</param>
    /// <param name="metadataJson">Optional flat JSON object with string metadata values.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <response code="200">Returns the extracted fields, labels, entities, clips, issues and confidence.</response>
    /// <response code="400">The request is invalid or the file cannot be processed.</response>
    /// <response code="401">A valid bearer token was not provided.</response>
    /// <response code="403">The token cannot submit work for the requested client or profile.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(104_857_600)]
    [ProducesResponseType(typeof(AnalysisResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Analyze(
        [FromForm] string clientKey,
        [FromForm] string profileKey,
        [FromForm] MediaAssetType assetType,
        IFormFile file,
        [FromForm] string? metadataJson,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new ApiErrorResponse("INVALID_REQUEST", ApiResourceText.FileRequired, HttpContext.TraceIdentifier));
        }

        if (!await accessService.CanSubmitAsync(User, clientKey, profileKey, cancellationToken))
        {
            return Forbid();
        }

        var maxFileSizeBytes = configuration.GetValue<long>("MediaAnalysis:MaxFileSizeBytes", 104_857_600);
        if (file.Length > maxFileSizeBytes)
        {
            return BadRequest(new ApiErrorResponse("INVALID_REQUEST", ApiResourceText.FileExceedsMaxSize(maxFileSizeBytes), HttpContext.TraceIdentifier));
        }

        var metadata = ParseMetadata(metadataJson);
        await using var stream = file.OpenReadStream();

        var response = await service.AnalyzeAsync(
            new CreateAnalysisJobRequest(clientKey, profileKey, assetType, file.FileName, file.ContentType, metadata),
            stream,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Parses optional multipart metadata as a flat string dictionary.
    /// </summary>
    private static Dictionary<string, string>? ParseMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException(ApiResourceText.MetadataJsonMustBeObject);
            }

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    throw new ArgumentException(ApiResourceText.MetadataJsonOnlyStringValues);
                }

                metadata[property.Name] = property.Value.GetString() ?? string.Empty;
            }

            return metadata;
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(ApiResourceText.MetadataJsonMustBeValidJson, exception);
        }
    }
}
