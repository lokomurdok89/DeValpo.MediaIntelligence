namespace DeValpo.MediaIntelligence.Infrastructure.Options;

public sealed class StorageOptions
{
    public string Provider { get; init; } = "Local";
    public string LocalRootPath { get; init; } = "storage/media-assets";
}
