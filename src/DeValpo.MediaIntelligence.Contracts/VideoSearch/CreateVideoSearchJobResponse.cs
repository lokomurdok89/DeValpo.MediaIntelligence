namespace DeValpo.MediaIntelligence.Contracts.VideoSearch;

public sealed record CreateVideoSearchJobResponse(
    Guid JobId,
    string Status,
    DateTimeOffset CreatedAt);
