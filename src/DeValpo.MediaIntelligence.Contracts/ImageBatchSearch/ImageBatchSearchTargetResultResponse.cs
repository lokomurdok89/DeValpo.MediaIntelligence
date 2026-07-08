namespace DeValpo.MediaIntelligence.Contracts.ImageBatchSearch;

public sealed record ImageBatchSearchTargetResultResponse(
    string TargetKey,
    string QueryType,
    IReadOnlyCollection<ImageBatchSearchMatchResponse> Matches);
