namespace DeValpo.MediaIntelligence.Application.VideoSearch;

public sealed record CreateVideoSearchJobCommand(
    string ClientKey,
    string ProfileKey,
    string ManifestJson,
    IReadOnlyCollection<VideoSearchUpload> Videos);
