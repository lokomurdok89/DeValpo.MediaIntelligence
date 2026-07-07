namespace DeValpo.MediaIntelligence.Application.VideoSearch;

public sealed record VideoSearchUpload(
    string OriginalFileName,
    string ContentType,
    Stream Content);
