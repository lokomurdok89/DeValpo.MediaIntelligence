namespace DeValpo.MediaIntelligence.Application.ImageBatchSearch;

public sealed class ImageBatchSearchOptions
{
    public bool Enabled { get; init; }
    public int MaxImagesPerJob { get; init; } = 1000;
    public int MaxTargetsPerJob { get; init; } = 200;
    public long MaxImageSizeBytes { get; init; } = 104_857_600;
    public bool AwsProviderEnabled { get; init; } = true;
    public bool GoogleVisionProviderEnabled { get; init; } = true;
    public decimal DefaultMinConfidence { get; init; } = 0.80m;
    public decimal DefaultMinSimilarity { get; init; } = 90m;
}
