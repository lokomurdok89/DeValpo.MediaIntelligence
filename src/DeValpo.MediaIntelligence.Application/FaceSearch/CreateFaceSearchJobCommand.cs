using DeValpo.MediaIntelligence.Application.VideoSearch;

namespace DeValpo.MediaIntelligence.Application.FaceSearch;

public sealed record CreateFaceSearchJobCommand(
    string ClientKey,
    string ProfileKey,
    string ConsentReferenceId,
    string ManifestJson,
    IReadOnlyCollection<VideoSearchUpload> Videos,
    IReadOnlyCollection<VideoSearchUpload> ReferenceImages);
