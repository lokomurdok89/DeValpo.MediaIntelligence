namespace DeValpo.MediaIntelligence.Application.ImageBatchSearch;

public sealed record ImageBatchSearchUpload(
    string OriginalFileName,
    string ContentType,
    Stream Content);
