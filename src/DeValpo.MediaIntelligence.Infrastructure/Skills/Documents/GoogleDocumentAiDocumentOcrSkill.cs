using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Skills;
using Google.Cloud.DocumentAI.V1;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills.Documents;

public sealed class GoogleDocumentAiDocumentOcrSkill(
    IOptions<GoogleDocumentAiOptions> options,
    IGoogleDocumentAiClientFactory clientFactory) : BaseMockSkill
{
    public override string SkillName => nameof(GoogleDocumentAiDocumentOcrSkill);
    public override IReadOnlyCollection<MediaAssetType> SupportedAssetTypes => [MediaAssetType.Document];
    public override IReadOnlyCollection<string> RequiredCapabilities => ["OCR", "DocumentExtraction"];
    public override string Description => "Google Document AI OCR provider boundary. It is disabled unless configured.";

    public override Task<SkillExecutionResult> ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken)
    {
        var config = options.Value;

        if (!config.Enabled)
        {
            return Task.FromResult(Result(
                SkillName,
                0.0m,
                issues:
                [
                    new("GOOGLE_DOCUMENT_AI_DISABLED", "Google Document AI is disabled by configuration.", "Warning", SkillName)
                ]));
        }

        var missing = new[]
            {
                string.IsNullOrWhiteSpace(config.ProjectId) ? "ProjectId" : null,
                string.IsNullOrWhiteSpace(config.Location) ? "Location" : null,
                string.IsNullOrWhiteSpace(config.ProcessorId) ? "ProcessorId" : null
            }
            .Where(item => item is not null)
            .ToArray();

        if (missing.Length > 0)
        {
            return Task.FromResult(Result(
                SkillName,
                0.0m,
                issues:
                [
                    new("GOOGLE_DOCUMENT_AI_NOT_CONFIGURED", $"Missing Google Document AI options: {string.Join(", ", missing)}.", "Error", SkillName)
                ]));
        }

        return ExecuteGoogleDocumentAiAsync(config, context, cancellationToken);
    }

    private async Task<SkillExecutionResult> ExecuteGoogleDocumentAiAsync(
        GoogleDocumentAiOptions config,
        SkillExecutionContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = await clientFactory.CreateAsync(config, cancellationToken);
            var processorName = clientFactory.CreateProcessorName(config);

            if (context.Content.CanSeek)
            {
                context.Content.Position = 0;
            }

            var response = await client.ProcessDocumentAsync(
                new ProcessRequest
                {
                    Name = processorName.ToString(),
                    RawDocument = new RawDocument
                    {
                        Content = await ByteString.FromStreamAsync(context.Content, cancellationToken),
                        MimeType = ResolveMimeType(context)
                    }
                },
                cancellationToken);

            var text = response.Document?.Text ?? string.Empty;

            return Result(
                SkillName,
                0.89m,
                fields:
                [
                    new("provider", "google-document-ai", 1.00m, SkillName),
                    new("processor_id", config.ProcessorId, 0.89m, SkillName),
                    new("text", text, string.IsNullOrWhiteSpace(text) ? 0.50m : 0.89m, SkillName)
                ]);
        }
        catch (Exception exception)
        {
            return Result(
                SkillName,
                0.0m,
                issues:
                [
                    new("GOOGLE_DOCUMENT_AI_UNAVAILABLE", ClassifyExecutionError(exception), "Error", SkillName)
                ]);
        }
    }

    private static string ResolveMimeType(SkillExecutionContext context)
    {
        if (context.Metadata.TryGetValue("contentType", out var contentType) &&
            !string.IsNullOrWhiteSpace(contentType))
        {
            return contentType;
        }

        return context.AssetType == MediaAssetType.Document ? "application/pdf" : "application/octet-stream";
    }

    private static string ClassifyExecutionError(Exception exception)
    {
        return exception switch
        {
            FileNotFoundException => "Google Document AI credentials file was not found.",
            UnauthorizedAccessException => "Google Document AI credentials file cannot be read.",
            InvalidOperationException => "Google Document AI credentials are invalid.",
            RpcException { StatusCode: StatusCode.Unauthenticated } =>
                "Google Document AI credentials are invalid or expired.",
            RpcException { StatusCode: StatusCode.PermissionDenied } =>
                "Google Document AI service account does not have permission for this processor.",
            RpcException { StatusCode: StatusCode.NotFound } =>
                "Google Document AI processor was not found.",
            RpcException { StatusCode: StatusCode.FailedPrecondition } =>
                "Google Document AI API or billing is not available for this project.",
            RpcException { StatusCode: StatusCode.ResourceExhausted } =>
                "Google Document AI quota or billing limit was reached.",
            RpcException { StatusCode: StatusCode.Unavailable } =>
                "Google Document AI service is unavailable.",
            RpcException rpcException =>
                $"Google Document AI request failed: {rpcException.StatusCode}.",
            _ => "Google Document AI request failed."
        };
    }
}
