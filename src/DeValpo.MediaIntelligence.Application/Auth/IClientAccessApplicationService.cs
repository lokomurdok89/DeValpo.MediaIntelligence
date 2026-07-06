using DeValpo.MediaIntelligence.Contracts.Auth;

namespace DeValpo.MediaIntelligence.Application.Auth;

public interface IClientAccessApplicationService
{
    /// <summary>
    /// Creates a client and returns its initial API key exactly once.
    /// </summary>
    Task<ApiClientWithKeyResponse> CreateClientAsync(
        CreateApiClientRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Revokes an existing key and returns a replacement API key exactly once.
    /// </summary>
    Task<ApiKeyCreatedResponse> RotateApiKeyAsync(
        string clientKey,
        Guid apiKeyId,
        RotateApiKeyRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Revokes an API key so it can no longer mint tokens.
    /// </summary>
    Task RevokeApiKeyAsync(
        string clientKey,
        Guid apiKeyId,
        CancellationToken cancellationToken);
}
