namespace DeValpo.MediaIntelligence.Contracts.Retention;

public sealed record RetentionExecutionResponse(
    string ClientKey,
    int ScannedAssets,
    int DeletedAssets,
    DateTimeOffset Cutoff,
    DateTimeOffset ExecutedAt);
