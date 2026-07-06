using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IMediaAnalysisSkill
{
    /// <summary>
    /// Stable skill name used by profiles to invoke this capability.
    /// </summary>
    string SkillName { get; }

    /// <summary>
    /// Media types accepted by the skill.
    /// </summary>
    IReadOnlyCollection<MediaAssetType> SupportedAssetTypes { get; }

    /// <summary>
    /// Capability keys required before the skill can be considered usable.
    /// </summary>
    IReadOnlyCollection<string> RequiredCapabilities { get; }

    /// <summary>
    /// Human-readable skill description exposed by the API.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Processes one media asset and returns fields, labels, clips, entities and issues.
    /// </summary>
    Task<SkillExecutionResult> ExecuteAsync(
        SkillExecutionContext context,
        CancellationToken cancellationToken);
}
