using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Infrastructure.Resources;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills;

public sealed class MockImageObjectDetectionSkill : BaseMockSkill
{
    public override string SkillName => nameof(MockImageObjectDetectionSkill);
    public override IReadOnlyCollection<MediaAssetType> SupportedAssetTypes => [MediaAssetType.Image, MediaAssetType.Video, MediaAssetType.LiveVideo];
    public override IReadOnlyCollection<string> RequiredCapabilities => ["ObjectDetection", "PersonDetection"];
    public override string Description => InfrastructureResourceText.MockImageObjectDetectionDescription;

    /// <summary>
    /// Produces deterministic object and person detections for scaffolding.
    /// </summary>
    public override Task<SkillExecutionResult> ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result(
            SkillName,
            0.79m,
            entities:
            [
                new("person", "person-1", 0.79m, SkillName)
            ],
            labels:
            [
                new("outdoor-event", 0.76m, SkillName)
            ]));
    }
}
