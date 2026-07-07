namespace DeValpo.MediaIntelligence.Application.VideoSearch;

public sealed record VideoSearchManifest(
    IReadOnlyCollection<VideoSearchTarget> Targets,
    bool DetectFaces,
    bool DetectNumbers,
    decimal MinConfidence,
    decimal MergeGapSeconds);

public sealed record VideoSearchTarget(
    string TargetKey,
    string VisibleNumber);
