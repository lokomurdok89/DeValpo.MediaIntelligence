namespace DeValpo.MediaIntelligence.Contracts.FaceSearch;

public sealed record FaceSearchMatchSegmentResponse(
    string TargetKey,
    string ReferenceImageKey,
    string VideoKey,
    string VideoFileName,
    decimal StartTimeSeconds,
    decimal EndTimeSeconds,
    string StartTimecode,
    string EndTimecode,
    decimal Similarity,
    string MatchType,
    string SourceProvider);
