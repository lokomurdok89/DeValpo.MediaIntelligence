using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Application.Resources;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Application.MediaAnalysis;

public sealed class MediaAnalysisOrchestrator(
    IAnalysisProfileResolver profileResolver,
    IEnumerable<IMediaAnalysisSkill> skills) : IMediaAnalysisOrchestrator
{
    /// <summary>
    /// Resolves the requested profile, executes its skills in order and determines review status.
    /// </summary>
    public async Task<AnalysisOrchestrationResult> AnalyzeAsync(
        AnalysisOrchestrationRequest request,
        Stream content,
        CancellationToken cancellationToken)
    {
        var profile = await profileResolver.ResolveAsync(request.ProfileKey, cancellationToken)
            ?? throw new InvalidOperationException(ApplicationResourceText.ProfileNotFound(request.ProfileKey));

        if (!profile.SupportedAssetTypes.Contains(request.AssetType))
        {
            throw new InvalidOperationException(ApplicationResourceText.ProfileAssetTypeUnsupported(request.ProfileKey, request.AssetType));
        }

        var skillsByName = skills.ToDictionary(skill => skill.SkillName, StringComparer.OrdinalIgnoreCase);
        var duplicateSkillName = skills
            .GroupBy(skill => skill.SkillName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;

        if (duplicateSkillName is not null)
        {
            throw new InvalidOperationException(ApplicationResourceText.DuplicateSkillName(duplicateSkillName));
        }

        var results = new List<SkillExecutionResult>();
        var issues = new List<AnalysisIssueResponse>();

        foreach (var skillName in profile.SkillNames)
        {
            if (!skillsByName.TryGetValue(skillName, out var skill))
            {
                issues.Add(new AnalysisIssueResponse("SKILL_NOT_REGISTERED", ApplicationResourceText.SkillNotRegistered(skillName), "Error", "Orchestrator"));
                continue;
            }

            if (!skill.SupportedAssetTypes.Contains(request.AssetType))
            {
                issues.Add(new AnalysisIssueResponse("SKILL_ASSET_TYPE_UNSUPPORTED", ApplicationResourceText.SkillAssetTypeUnsupported(skill.SkillName, request.AssetType), "Warning", skill.SkillName));
                continue;
            }

            if (content.CanSeek)
            {
                content.Position = 0;
            }

            var context = new SkillExecutionContext(
                request.JobId,
                request.ClientKey,
                request.ProfileKey,
                request.AssetType,
                content,
                request.Metadata);

            var result = await skill.ExecuteAsync(context, cancellationToken);
            results.Add(result);
            issues.AddRange(result.Issues);
        }

        var confidence = results.Count == 0 ? 0m : decimal.Round(results.Average(item => item.Confidence), 4);
        var requiresHumanReview = profile.HumanReviewEnabled && (confidence < profile.MinimumConfidence || issues.Any(item => item.Severity == "Error"));

        if (requiresHumanReview)
        {
            issues.Add(new AnalysisIssueResponse("HUMAN_REVIEW_REQUIRED", ApplicationResourceText.HumanReviewRequiredIssue, "Warning", "Orchestrator"));
        }

        return new AnalysisOrchestrationResult(confidence, requiresHumanReview, results, issues);
    }
}
