namespace DeValpo.MediaIntelligence.Contracts.VideoSearch;

public sealed record VideoTargetResultResponse(
    string TargetKey,
    string VisibleNumber,
    IReadOnlyCollection<VideoTargetMatchSegmentResponse> Matches);
