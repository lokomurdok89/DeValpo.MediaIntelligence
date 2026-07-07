namespace DeValpo.MediaIntelligence.Application.FaceSearch;

public sealed record FaceSearchManifest(
    IReadOnlyCollection<FaceSearchTarget> Targets,
    decimal MinSimilarity);

public sealed record FaceSearchTarget(
    string TargetKey,
    string ReferenceImageKey);
