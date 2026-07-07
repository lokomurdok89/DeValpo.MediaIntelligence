namespace DeValpo.MediaIntelligence.Contracts.Retention;

public sealed record RetentionPolicyRequest(
    bool Enabled,
    int RetentionDays);
