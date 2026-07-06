using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.MediaAnalysis;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Application.Profiles;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Infrastructure.Skills;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class MediaAnalysisOrchestratorTests
{
    [Fact]
    public async Task AnalyzeAsync_WithArenysmedProfile_ReturnsNormalizedResultAboveThreshold()
    {
        var orchestrator = CreateOrchestrator(
            new MockDocumentOcrSkill(),
            new MockHandwrittenIdExtractionSkill(),
            new MockStampDetectionSkill());

        await using var content = new MemoryStream([1, 2, 3]);

        var result = await orchestrator.AnalyzeAsync(
            new AnalysisOrchestrationRequest(
                Guid.NewGuid(),
                "arenysmed",
                "arenysmed-delivery-evidence-v1",
                MediaAssetType.Image,
                new Dictionary<string, string>()),
            content,
            CancellationToken.None);

        result.SkillResults.Should().HaveCount(3);
        result.Confidence.Should().Be(0.8667m);
        result.RequiresHumanReview.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeAsync_WhenProfileDoesNotSupportAssetType_ThrowsClearError()
    {
        var orchestrator = CreateOrchestrator(new MockDocumentOcrSkill());
        await using var content = new MemoryStream([1, 2, 3]);

        var act = () => orchestrator.AnalyzeAsync(
            new AnalysisOrchestrationRequest(
                Guid.NewGuid(),
                "generic",
                "generic-document-extraction-v1",
                MediaAssetType.Video,
                new Dictionary<string, string>()),
            content,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not support asset type*");
    }

    private static IMediaAnalysisOrchestrator CreateOrchestrator(params IMediaAnalysisSkill[] skills)
    {
        var profileResolver = new StaticAnalysisProfileResolver(
            Options.Create(new AnalysisProfileOptions { EnableMockProfiles = true }));

        return new MediaAnalysisOrchestrator(profileResolver, skills);
    }
}
