using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Infrastructure.Resources;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills;

public sealed class MockSportsBibDetectionSkill : BaseMockSkill
{
    public override string SkillName => nameof(MockSportsBibDetectionSkill);
    public override IReadOnlyCollection<MediaAssetType> SupportedAssetTypes => [MediaAssetType.Image, MediaAssetType.Video, MediaAssetType.LiveVideo];
    public override IReadOnlyCollection<string> RequiredCapabilities => ["BibNumberDetection"];
    public override string Description => InfrastructureResourceText.MockSportsBibDetectionDescription;

    /// <summary>
    /// Produces a deterministic bib number field for scaffolding.
    /// </summary>
    public override Task<SkillExecutionResult> ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result(
            SkillName,
            0.84m,
            fields:
            [
                new("bib_number", "2048", 0.84m, SkillName)
            ],
            entities:
            [
                new("bib", "2048", 0.84m, SkillName)
            ]));
    }
}
