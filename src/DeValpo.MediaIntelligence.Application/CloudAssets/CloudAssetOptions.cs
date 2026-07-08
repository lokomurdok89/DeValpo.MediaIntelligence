namespace DeValpo.MediaIntelligence.Application.CloudAssets;

public sealed class CloudAssetOptions
{
    public string[] AllowedS3Buckets { get; init; } = [];
    public string[] AllowedGcsBuckets { get; init; } = [];
    public int DefaultStagingTtlDays { get; init; } = 3;
    public int MinStagingTtlDays { get; init; } = 1;
    public int MaxStagingTtlDays { get; init; } = 7;
    public bool MonthlySweepEnabled { get; init; } = true;
    public int MaxAssetsPerJob { get; init; } = 10_000;
    public int MaxVideosPerJob { get; init; } = 100;
    public int MaxImagesPerJob { get; init; } = 10_000;
    public int MaxTargetsPerJob { get; init; } = 200;
    public int MaxConcurrentJobsPerClient { get; init; } = 5;
    public int MaxConcurrentProviderCallsPerClient { get; init; } = 10;
    public int? MaxVideoDurationSeconds { get; init; } = 14_400;
    public long? MaxAssetSizeBytes { get; init; }
}
