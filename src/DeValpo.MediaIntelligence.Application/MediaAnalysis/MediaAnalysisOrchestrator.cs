using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Application.MediaAnalysis;

public sealed class MediaAnalysisOrchestrator(
    IAnalysisProfileResolver profileResolver,
    IEnumerable<IMediaAnalysisSkill> skills) : IMediaAnalysisOrchestrator
{
    public async Task<AnalysisOrchestrationResult> AnalyzeAsync(
        AnalysisOrchestrationRequest request,
        Stream content,
        CancellationToken cancellationToken)
    {
        var profile = await profileResolver.ResolveAsync(request.ProfileKey, cancellationToken)
            ?? throw new InvalidOperationException($"Profile '{request.ProfileKey}' was not found.");

        if (!profile.SupportedAssetTypes.Contains(request.AssetType))
        {
            throw new InvalidOperationException($"Profile '{request.ProfileKey}' does not support asset type '{request.AssetType}'.");
        }

        var skillsByName = skills.ToDictionary(skill => skill.SkillName, StringComparer.OrdinalIgnoreCase);
        var duplicateSkillName = skills
            .GroupBy(skill => skill.SkillName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;

        if (duplicateSkillName is not null)
        {
            throw new InvalidOperationException($"Duplicate skill name registered: {duplicateSkillName}.");
        }

        var results = new List<SkillExecutionResult>();
        var issues = new List<AnalysisIssueResponse>();

        foreach (var skillName in profile.SkillNames)
        {
            if (!skillsByName.TryGetValue(skillName, out var skill))
            {
                issues.Add(new AnalysisIssueResponse("SKILL_NOT_REGISTERED", $"Skill '{skillName}' is not registered.", "Error", "Orchestrator"));
                continue;
            }

            if (!skill.SupportedAssetTypes.Contains(request.AssetType))
            {
                issues.Add(new AnalysisIssueResponse("SKILL_ASSET_TYPE_UNSUPPORTED", $"Skill '{skill.SkillName}' does not support '{request.AssetType}'.", "Warning", skill.SkillName));
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
            issues.Add(new AnalysisIssueResponse("HUMAN_REVIEW_REQUIRED", "Confidence or execution issues require human review.", "Warning", "Orchestrator"));
        }

        return new AnalysisOrchestrationResult(confidence, requiresHumanReview, results, issues);
    }
}
