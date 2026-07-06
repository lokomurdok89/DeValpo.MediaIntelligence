using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;

namespace DeValpo.MediaIntelligence.Application.Models;

public sealed record SkillExecutionContext(
    Guid JobId,
    string ClientKey,
    string ProfileKey,
    MediaAssetType AssetType,
    Stream Content,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record SkillExecutionResult(
    string SkillName,
    decimal Confidence,
    IReadOnlyCollection<ExtractedFieldResponse> ExtractedFields,
    IReadOnlyCollection<DetectedEntityResponse> DetectedEntities,
    IReadOnlyCollection<DetectedLabelResponse> DetectedLabels,
    IReadOnlyCollection<GeneratedClipResponse> GeneratedClips,
    IReadOnlyCollection<AnalysisIssueResponse> Issues);

public sealed record AnalysisOrchestrationRequest(
    Guid JobId,
    string ClientKey,
    string ProfileKey,
    MediaAssetType AssetType,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record AnalysisOrchestrationResult(
    decimal Confidence,
    bool RequiresHumanReview,
    IReadOnlyCollection<SkillExecutionResult> SkillResults,
    IReadOnlyCollection<AnalysisIssueResponse> Issues);

public sealed record StoredMediaAsset(
    string StoragePath,
    string FileName,
    string ContentType,
    long SizeInBytes);
