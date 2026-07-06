using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Skills;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.DocumentAI.V1;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills.Documents;

public sealed class GoogleDocumentAiDocumentOcrSkill(IOptions<GoogleDocumentAiOptions> options) : BaseMockSkill
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
        var builder = new DocumentProcessorServiceClientBuilder();
        if (!string.IsNullOrWhiteSpace(config.CredentialsPath))
        {
            var credential = await CredentialFactory.FromFileAsync<ServiceAccountCredential>(
                config.CredentialsPath,
                cancellationToken);
            builder.GoogleCredential = credential.ToGoogleCredential();
        }

        var client = await builder.BuildAsync(cancellationToken);
        var processorName = ProcessorName.FromProjectLocationProcessor(
            config.ProjectId,
            config.Location,
            config.ProcessorId);

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

    private static string ResolveMimeType(SkillExecutionContext context)
    {
        if (context.Metadata.TryGetValue("contentType", out var contentType) &&
            !string.IsNullOrWhiteSpace(contentType))
        {
            return contentType;
        }

        return context.AssetType == MediaAssetType.Document ? "application/pdf" : "application/octet-stream";
    }
}
