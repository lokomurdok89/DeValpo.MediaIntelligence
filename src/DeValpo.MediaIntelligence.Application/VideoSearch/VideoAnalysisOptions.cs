namespace DeValpo.MediaIntelligence.Application.VideoSearch;

public sealed class VideoAnalysisOptions
{
    public int MaxVideosPerJob { get; init; } = 5;
    public long MaxVideoSizeBytes { get; init; } = 1_073_741_824;
    public int MaxDurationSeconds { get; init; } = 3_600;
    public int MaxProcessingAttempts { get; init; } = 3;
    public decimal DefaultMinConfidence { get; init; } = 0.70m;
    public decimal DefaultMergeGapSeconds { get; init; } = 2m;
    public bool FaceMatchingEnabled { get; init; }
}
