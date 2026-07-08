namespace DeValpo.MediaIntelligence.Contracts.ImageBatchSearch;

public sealed record CreateImageBatchSearchJobResponse(
    Guid JobId,
    string Status,
    DateTimeOffset CreatedAt);
