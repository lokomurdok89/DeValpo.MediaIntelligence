using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Infrastructure.Resources;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills;

public sealed class MockDocumentOcrSkill : BaseMockSkill
{
    public override string SkillName => nameof(MockDocumentOcrSkill);
    public override IReadOnlyCollection<MediaAssetType> SupportedAssetTypes => [MediaAssetType.Document, MediaAssetType.Image];
    public override IReadOnlyCollection<string> RequiredCapabilities => ["OCR", "DocumentExtraction"];
    public override string Description => InfrastructureResourceText.MockDocumentOcrDescription;

    /// <summary>
    /// Produces deterministic document OCR fields for non-production scaffolding.
    /// </summary>
    public override Task<SkillExecutionResult> ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result(
            SkillName,
            0.91m,
            fields:
            [
                new("visible_text", InfrastructureResourceText.MockExtractedText, 0.91m, SkillName),
                new("document_type", context.AssetType == MediaAssetType.Document ? "document" : "image", 0.88m, SkillName)
            ],
            labels:
            [
                new("document", 0.90m, SkillName)
            ]));
    }
}
