namespace DeValpo.MediaIntelligence.Contracts.FaceSearch;

public sealed record CreateFaceSearchJobResponse(
    Guid JobId,
    string Status,
    DateTimeOffset CreatedAt);
