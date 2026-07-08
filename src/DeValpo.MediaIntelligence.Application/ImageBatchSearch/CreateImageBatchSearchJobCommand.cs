using DeValpo.MediaIntelligence.Application.CloudAssets;

namespace DeValpo.MediaIntelligence.Application.ImageBatchSearch;

public sealed record CreateImageBatchSearchJobCommand(
    string ClientKey,
    string ProfileKey,
    string ManifestJson,
    IReadOnlyCollection<ImageBatchSearchUpload> Images,
    IReadOnlyCollection<ImageBatchSearchUpload> ReferenceImages,
    CloudAssetSelector? AssetSelector = null,
    CloudAssetSelector? ReferenceAssetSelector = null);
