namespace DeValpo.MediaIntelligence.Contracts.ImageBatchSearch;

public sealed record ImageBatchSearchMatchResponse(
    string ImageKey,
    string FileName,
    decimal Score,
    string? DetectedText,
    string SourceProvider);
