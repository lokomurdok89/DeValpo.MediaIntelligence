namespace DeValpo.MediaIntelligence.Contracts.VideoSearch;

public sealed record VideoTargetMatchSegmentResponse(
    string TargetKey,
    string VisibleNumber,
    string VideoKey,
    string VideoFileName,
    decimal StartTimeSeconds,
    decimal EndTimeSeconds,
    string StartTimecode,
    string EndTimecode,
    decimal Confidence,
    string SourceProvider);
