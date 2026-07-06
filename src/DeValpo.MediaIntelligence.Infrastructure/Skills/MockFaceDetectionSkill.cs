using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Infrastructure.Resources;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills;

public sealed class MockFaceDetectionSkill : BaseMockSkill
{
    public override string SkillName => nameof(MockFaceDetectionSkill);
    public override IReadOnlyCollection<MediaAssetType> SupportedAssetTypes => [MediaAssetType.Image, MediaAssetType.Video];
    public override IReadOnlyCollection<string> RequiredCapabilities => ["FaceDetection"];
    public override string Description => InfrastructureResourceText.MockFaceDetectionDescription;

    /// <summary>
    /// Returns an informational issue because real face detection is intentionally disabled.
    /// </summary>
    public override Task<SkillExecutionResult> ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result(
            SkillName,
            0.70m,
            issues:
            [
                new("FACE_DETECTION_DISABLED", InfrastructureResourceText.FaceDetectionDisabled, "Info", SkillName)
            ]));
    }
}
