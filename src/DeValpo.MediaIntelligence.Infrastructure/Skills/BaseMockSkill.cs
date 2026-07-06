using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills;

public abstract class BaseMockSkill : IMediaAnalysisSkill
{
    public abstract string SkillName { get; }
    public abstract IReadOnlyCollection<MediaAssetType> SupportedAssetTypes { get; }
    public abstract IReadOnlyCollection<string> RequiredCapabilities { get; }
    public abstract string Description { get; }

    public abstract Task<SkillExecutionResult> ExecuteAsync(
        SkillExecutionContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a skill result while normalizing optional collections to empty arrays.
    /// </summary>
    protected static SkillExecutionResult Result(
        string skillName,
        decimal confidence,
        IReadOnlyCollection<ExtractedFieldResponse>? fields = null,
        IReadOnlyCollection<DetectedEntityResponse>? entities = null,
        IReadOnlyCollection<DetectedLabelResponse>? labels = null,
        IReadOnlyCollection<GeneratedClipResponse>? clips = null,
        IReadOnlyCollection<AnalysisIssueResponse>? issues = null)
    {
        return new SkillExecutionResult(
            skillName,
            confidence,
            fields ?? [],
            entities ?? [],
            labels ?? [],
            clips ?? [],
            issues ?? []);
    }
}
