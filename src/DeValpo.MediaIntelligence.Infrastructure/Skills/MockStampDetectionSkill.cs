using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills;

public sealed class MockStampDetectionSkill : BaseMockSkill
{
    public override string SkillName => nameof(MockStampDetectionSkill);
    public override IReadOnlyCollection<MediaAssetType> SupportedAssetTypes => [MediaAssetType.Document, MediaAssetType.Image];
    public override IReadOnlyCollection<string> RequiredCapabilities => ["StampDetection"];
    public override string Description => "Deterministic mock stamp detection.";

    public override Task<SkillExecutionResult> ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result(
            SkillName,
            0.87m,
            labels:
            [
                new("stamp:delivered", 0.87m, SkillName)
            ]));
    }
}
