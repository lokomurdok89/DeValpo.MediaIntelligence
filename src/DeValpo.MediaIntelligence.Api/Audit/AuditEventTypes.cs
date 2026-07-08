namespace DeValpo.MediaIntelligence.Api.Audit;

internal static class AuditEventTypes
{
    public const string AuthTokenIssued = "auth.token_issued";
    public const string AuthTokenRejected = "auth.token_rejected";
    public const string ApiClientCreated = "api_client.created";
    public const string ApiKeyRotated = "api_key.rotated";
    public const string ApiKeyRevoked = "api_key.revoked";
    public const string AnalysisResultRead = "analysis_result.read";
    public const string FaceSearchCreated = "face_search.created";
    public const string FaceSearchResultRead = "face_search.result_read";
    public const string HumanReviewStarted = "human_review.started";
    public const string HumanReviewCompleted = "human_review.completed";
    public const string WebhookTargetCreated = "webhook_target.created";
    public const string RetentionPolicyUpdated = "retention_policy.updated";
    public const string RetentionExecuted = "retention.executed";
}
