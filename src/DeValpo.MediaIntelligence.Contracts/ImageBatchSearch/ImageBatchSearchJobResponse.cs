namespace DeValpo.MediaIntelligence.Contracts.ImageBatchSearch;

public sealed record ImageBatchSearchJobResponse(
    Guid JobId,
    string ClientKey,
    string ProfileKey,
    string Status,
    int ProcessingAttempts,
    int ImageCount,
    int ReferenceImageCount,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
