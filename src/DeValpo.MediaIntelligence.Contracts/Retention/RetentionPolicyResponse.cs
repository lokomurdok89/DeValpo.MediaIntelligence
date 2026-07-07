namespace DeValpo.MediaIntelligence.Contracts.Retention;

public sealed record RetentionPolicyResponse(
    string ClientKey,
    bool Enabled,
    int RetentionDays,
    DateTimeOffset UpdatedAt);
