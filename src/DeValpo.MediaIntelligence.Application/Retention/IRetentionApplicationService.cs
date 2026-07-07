using DeValpo.MediaIntelligence.Contracts.Retention;

namespace DeValpo.MediaIntelligence.Application.Retention;

public interface IRetentionApplicationService
{
    /// <summary>
    /// Returns the retention policy for a client.
    /// </summary>
    Task<RetentionPolicyResponse?> GetPolicyAsync(string clientKey, CancellationToken cancellationToken);

    /// <summary>
    /// Creates or updates the retention policy for a client.
    /// </summary>
    Task<RetentionPolicyResponse> UpsertPolicyAsync(
        string clientKey,
        RetentionPolicyRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Applies the client's retention policy to stored media assets.
    /// </summary>
    Task<RetentionExecutionResponse> ExecuteAsync(string clientKey, CancellationToken cancellationToken);
}
