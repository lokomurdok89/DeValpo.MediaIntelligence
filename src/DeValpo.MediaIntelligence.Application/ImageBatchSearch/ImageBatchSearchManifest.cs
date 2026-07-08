namespace DeValpo.MediaIntelligence.Application.ImageBatchSearch;

public sealed record ImageBatchSearchManifest(
    IReadOnlyCollection<ImageBatchSearchTarget> Targets,
    IReadOnlyCollection<string> Providers);

public sealed record ImageBatchSearchTarget(
    string TargetKey,
    string QueryType,
    string? VisibleNumber,
    string? ReferenceImageKey,
    decimal MinConfidence,
    decimal MinSimilarity);
