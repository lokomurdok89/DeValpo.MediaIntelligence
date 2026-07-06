using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills;

public sealed class MockLiveVideoBufferAnalysisSkill : BaseMockSkill
{
    public override string SkillName => nameof(MockLiveVideoBufferAnalysisSkill);
    public override IReadOnlyCollection<MediaAssetType> SupportedAssetTypes => [MediaAssetType.LiveVideo];
    public override IReadOnlyCollection<string> RequiredCapabilities => ["LiveVideoBuffering"];
    public override string Description => "Structural mock for future buffered live video analysis.";

    public override Task<SkillExecutionResult> ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result(
            SkillName,
            0.72m,
            issues:
            [
                new("LIVE_VIDEO_STRUCTURAL_ONLY", "Live video is represented structurally in the initial phase.", "Info", SkillName)
            ]));
    }
}
