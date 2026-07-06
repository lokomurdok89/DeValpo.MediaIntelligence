using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills;

public sealed class MockHandwrittenIdExtractionSkill : BaseMockSkill
{
    public override string SkillName => nameof(MockHandwrittenIdExtractionSkill);
    public override IReadOnlyCollection<MediaAssetType> SupportedAssetTypes => [MediaAssetType.Document, MediaAssetType.Image];
    public override IReadOnlyCollection<string> RequiredCapabilities => ["HandwritingRecognition"];
    public override string Description => "Deterministic mock extraction of a handwritten ID.";

    public override Task<SkillExecutionResult> ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result(
            SkillName,
            0.82m,
            fields:
            [
                new("handwritten_id", "A-12345", 0.82m, SkillName)
            ]));
    }
}
