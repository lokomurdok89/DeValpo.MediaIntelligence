namespace DeValpo.MediaIntelligence.Contracts.FaceSearch;

public sealed record FaceSearchJobResponse(
    Guid JobId,
    string ClientKey,
    string ProfileKey,
    string Status,
    int ProcessingAttempts,
    int VideoCount,
    int ReferenceImageCount,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
