using DeValpo.MediaIntelligence.Application.CloudAssets;

namespace DeValpo.MediaIntelligence.Application.VideoSearch;

public sealed record CreateVideoSearchJobCommand(
    string ClientKey,
    string ProfileKey,
    string ManifestJson,
    IReadOnlyCollection<VideoSearchUpload> Videos,
    CloudAssetSelector? AssetSelector = null);
