using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
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
    public override string Description => InfrastructureResourceText.GoogleDocumentAiSkillDescription;

    /// <summary>
    /// Validates provider configuration and runs OCR through Google Document AI when enabled.
    /// </summary>
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
                    new("GOOGLE_DOCUMENT_AI_DISABLED", InfrastructureResourceText.GoogleDocumentAiDisabledByConfiguration, "Warning", SkillName)
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
                    new("GOOGLE_DOCUMENT_AI_NOT_CONFIGURED", InfrastructureResourceText.MissingGoogleDocumentAiOptions(string.Join(", ", missing)), "Error", SkillName)
                ]));
        }

        return ExecuteGoogleDocumentAiAsync(config, context, cancellationToken);
    }

    /// <summary>
    /// Sends the current media stream to the configured Document AI processor and maps the OCR text.
    /// </summary>
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

    /// <summary>
    /// Resolves the MIME type passed to Document AI from request metadata or asset type.
    /// </summary>
    private static string ResolveMimeType(SkillExecutionContext context)
    {
        if (context.Metadata.TryGetValue("contentType", out var contentType) &&
            !string.IsNullOrWhiteSpace(contentType))
        {
            return contentType;
        }

        return context.AssetType == MediaAssetType.Document ? "application/pdf" : "application/octet-stream";
    }

    /// <summary>
    /// Converts Document AI execution failures into readable analysis issues.
    /// </summary>
    private static string ClassifyExecutionError(Exception exception)
    {
        return exception switch
        {
            FileNotFoundException => InfrastructureResourceText.GoogleDocumentAiCredentialsFileNotFound,
            UnauthorizedAccessException => InfrastructureResourceText.GoogleDocumentAiCredentialsCannotBeRead,
            InvalidOperationException => InfrastructureResourceText.GoogleDocumentAiCredentialsInvalid,
            RpcException { StatusCode: StatusCode.Unauthenticated } =>
                InfrastructureResourceText.GoogleDocumentAiCredentialsInvalidOrExpired,
            RpcException { StatusCode: StatusCode.PermissionDenied } =>
                InfrastructureResourceText.GoogleDocumentAiPermissionDenied,
            RpcException { StatusCode: StatusCode.NotFound } =>
                InfrastructureResourceText.GoogleDocumentAiProcessorNotFound,
            RpcException { StatusCode: StatusCode.FailedPrecondition } =>
                InfrastructureResourceText.GoogleDocumentAiApiOrBillingUnavailable,
            RpcException { StatusCode: StatusCode.ResourceExhausted } =>
                InfrastructureResourceText.GoogleDocumentAiQuotaReached,
            RpcException { StatusCode: StatusCode.Unavailable } =>
                InfrastructureResourceText.GoogleDocumentAiServiceUnavailable,
            RpcException rpcException =>
                InfrastructureResourceText.GoogleDocumentAiRequestFailedWithStatus(rpcException.StatusCode),
            _ => InfrastructureResourceText.GoogleDocumentAiRequestFailed
        };
    }
}
