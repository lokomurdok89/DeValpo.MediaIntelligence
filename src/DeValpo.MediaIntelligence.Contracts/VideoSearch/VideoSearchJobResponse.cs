namespace DeValpo.MediaIntelligence.Contracts.VideoSearch;

public sealed record VideoSearchJobResponse(
    Guid JobId,
    string ClientKey,
    string ProfileKey,
    string Status,
    int ProcessingAttempts,
    int VideoCount,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
