using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills;

public sealed class MockVideoClipGenerationSkill : BaseMockSkill
{
    public override string SkillName => nameof(MockVideoClipGenerationSkill);
    public override IReadOnlyCollection<MediaAssetType> SupportedAssetTypes => [MediaAssetType.Video, MediaAssetType.LiveVideo, MediaAssetType.Image];
    public override IReadOnlyCollection<string> RequiredCapabilities => ["VideoClipGeneration"];
    public override string Description => "Structural mock clip generation without FFmpeg.";

    public override Task<SkillExecutionResult> ExecuteAsync(SkillExecutionContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result(
            SkillName,
            0.78m,
            clips:
            [
                new("clip-0001", null, 0.78m, SkillName)
            ]));
    }
}
