using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IMediaAnalysisSkill
{
    string SkillName { get; }
    IReadOnlyCollection<MediaAssetType> SupportedAssetTypes { get; }
    IReadOnlyCollection<string> RequiredCapabilities { get; }
    string Description { get; }

    Task<SkillExecutionResult> ExecuteAsync(
        SkillExecutionContext context,
        CancellationToken cancellationToken);
}
