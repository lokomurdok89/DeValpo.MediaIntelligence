using System.Text.Json;
using DeValpo.MediaIntelligence.Application.MediaAnalysis;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisJobs;
using Microsoft.AspNetCore.Mvc;

namespace DeValpo.MediaIntelligence.Api.Controllers;

[ApiController]
[Route("api/v1/analyze")]
public sealed class AnalyzeController(
    IMediaAnalysisApplicationService service,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(104_857_600)]
    public async Task<IActionResult> Analyze(
        [FromForm] string clientKey,
        [FromForm] string profileKey,
        [FromForm] MediaAssetType assetType,
        [FromForm] IFormFile file,
        [FromForm] string? metadataJson,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(new ApiErrorResponse("INVALID_REQUEST", "File is required.", HttpContext.TraceIdentifier));
        }

        var maxFileSizeBytes = configuration.GetValue<long>("MediaAnalysis:MaxFileSizeBytes", 104_857_600);
        if (file.Length > maxFileSizeBytes)
        {
            return BadRequest(new ApiErrorResponse("INVALID_REQUEST", $"File exceeds max size of {maxFileSizeBytes} bytes.", HttpContext.TraceIdentifier));
        }

        var metadata = ParseMetadata(metadataJson);
        await using var stream = file.OpenReadStream();

        var response = await service.AnalyzeAsync(
            new CreateAnalysisJobRequest(clientKey, profileKey, assetType, file.FileName, file.ContentType, metadata),
            stream,
            cancellationToken);

        return Ok(response);
    }

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
                throw new ArgumentException("metadataJson must be a JSON object.");
            }

            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    throw new ArgumentException("metadataJson only supports string values.");
                }

                metadata[property.Name] = property.Value.GetString() ?? string.Empty;
            }

            return metadata;
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("metadataJson must be valid JSON.", exception);
        }
    }
}
