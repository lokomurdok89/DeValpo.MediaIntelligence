using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills;

public sealed class MockDocumentOcrSkill : BaseMockSkill
{
    public override string SkillName => nameof(MockDocumentOcrSkill);
    public override IReadOnlyCollection<MediaAssetType> SupportedAssetTypes => [MediaAssetType.Document, MediaAssetType.Image];
    public override IReadOnlyCollection<string> RequiredCapabilities => ["OCR", "DocumentExtraction"];
    public override string Description => "Deterministic mock OCR for documents and images.";

    public override Task<SkillExecutionResult> ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result(
            SkillName,
            0.91m,
            fields:
            [
                new("visible_text", "Mock extracted text", 0.91m, SkillName),
                new("document_type", context.AssetType == MediaAssetType.Document ? "document" : "image", 0.88m, SkillName)
            ],
            labels:
            [
                new("document", 0.90m, SkillName)
            ]));
    }
}
