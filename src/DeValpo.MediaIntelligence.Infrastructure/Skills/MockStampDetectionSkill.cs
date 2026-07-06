using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Infrastructure.Resources;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills;

public sealed class MockStampDetectionSkill : BaseMockSkill
{
    public override string SkillName => nameof(MockStampDetectionSkill);
    public override IReadOnlyCollection<MediaAssetType> SupportedAssetTypes => [MediaAssetType.Document, MediaAssetType.Image];
    public override IReadOnlyCollection<string> RequiredCapabilities => ["StampDetection"];
    public override string Description => InfrastructureResourceText.MockStampDetectionDescription;

    /// <summary>
    /// Produces a deterministic stamp label for scaffolding.
    /// </summary>
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
