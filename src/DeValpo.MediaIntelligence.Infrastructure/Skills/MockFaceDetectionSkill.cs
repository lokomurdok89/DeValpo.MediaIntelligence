using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills;

public sealed class MockFaceDetectionSkill : BaseMockSkill
{
    public override string SkillName => nameof(MockFaceDetectionSkill);
    public override IReadOnlyCollection<MediaAssetType> SupportedAssetTypes => [MediaAssetType.Image, MediaAssetType.Video];
    public override IReadOnlyCollection<string> RequiredCapabilities => ["FaceDetection"];
    public override string Description => "Structural mock for future consent-based face detection.";

    public override Task<SkillExecutionResult> ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result(
            SkillName,
            0.70m,
            issues:
            [
                new("FACE_DETECTION_DISABLED", "Real face detection is intentionally disabled in the initial phase.", "Info", SkillName)
            ]));
    }
}
