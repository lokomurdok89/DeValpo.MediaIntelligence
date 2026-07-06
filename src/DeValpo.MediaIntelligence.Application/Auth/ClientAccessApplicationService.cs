using System.Text.Json;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Resources;
using DeValpo.MediaIntelligence.Contracts.Auth;
using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Auth;

public sealed class ClientAccessApplicationService(
    IClientRepository clientRepository,
    IClientApiKeyRepository apiKeyRepository,
    IApiKeySecretGenerator secretGenerator,
    IApiKeyHasher keyHasher,
    IUnitOfWork unitOfWork) : IClientAccessApplicationService
{
    private const string DefaultKeyName = "default";
    private const string DefaultScope = "media-intelligence:client";
    private const int KeyPrefixLength = 16;

    /// <summary>
    /// Creates a client and its first hashed API key.
    /// </summary>
    public async Task<ApiClientWithKeyResponse> CreateClientAsync(
        CreateApiClientRequest request,
        CancellationToken cancellationToken)
    {
        ValidateClientRequest(request);

        var clientKey = request.ClientKey.Trim();
        var existingClient = await clientRepository.GetByKeyAsync(clientKey, cancellationToken);
        if (existingClient is not null)
        {
            throw new InvalidOperationException(ApplicationResourceText.ClientAlreadyExists(clientKey));
        }

        var allowedProfiles = NormalizeAllowedProfiles(request.AllowedProfileKeys);
        var client = new Client
        {
            ClientKey = clientKey,
            DisplayName = request.DisplayName.Trim(),
            ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim(),
            AllowedProfileKeysJson = JsonSerializer.Serialize(allowedProfiles)
        };

        var rawKey = secretGenerator.Generate();
        var apiKey = CreateApiKey(client.Id, rawKey, request.KeyName, request.Scope, request.ExpiresAt, rotatedFromKeyId: null);

        await clientRepository.AddAsync(client, cancellationToken);
        await apiKeyRepository.AddAsync(apiKey, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ApiClientWithKeyResponse(
            client.ClientKey,
            client.DisplayName,
            client.ContactEmail,
            allowedProfiles,
            apiKey.Id,
            apiKey.KeyPrefix,
            rawKey,
            apiKey.Scope,
            apiKey.ExpiresAt,
            apiKey.CreatedAt);
    }

    /// <summary>
    /// Revokes an existing API key and creates a replacement key.
    /// </summary>
    public async Task<ApiKeyCreatedResponse> RotateApiKeyAsync(
        string clientKey,
        Guid apiKeyId,
        RotateApiKeyRequest request,
        CancellationToken cancellationToken)
    {
        var existingKey = await GetOwnedApiKeyAsync(clientKey, apiKeyId, cancellationToken);
        EnsureKeyCanBeChanged(existingKey);

        var rawKey = secretGenerator.Generate();
        var replacement = CreateApiKey(
            existingKey.ClientId,
            rawKey,
            request.KeyName,
            request.Scope,
            request.ExpiresAt,
            existingKey.Id);

        existingKey.RevokedAt = DateTimeOffset.UtcNow;

        await apiKeyRepository.AddAsync(replacement, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ApiKeyCreatedResponse(
            replacement.Id,
            existingKey.Client!.ClientKey,
            replacement.KeyPrefix,
            rawKey,
            replacement.Scope,
            replacement.ExpiresAt,
            replacement.CreatedAt);
    }

    /// <summary>
    /// Revokes an API key without returning a replacement.
    /// </summary>
    public async Task RevokeApiKeyAsync(
        string clientKey,
        Guid apiKeyId,
        CancellationToken cancellationToken)
    {
        var apiKey = await GetOwnedApiKeyAsync(clientKey, apiKeyId, cancellationToken);
        EnsureKeyCanBeChanged(apiKey);

        apiKey.RevokedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private ClientApiKey CreateApiKey(
        Guid clientId,
        string rawKey,
        string? keyName,
        string? scope,
        DateTimeOffset? expiresAt,
        Guid? rotatedFromKeyId)
    {
        return new ClientApiKey
        {
            ClientId = clientId,
            Name = string.IsNullOrWhiteSpace(keyName) ? DefaultKeyName : keyName.Trim(),
            KeyPrefix = rawKey[..Math.Min(KeyPrefixLength, rawKey.Length)],
            KeyHash = keyHasher.Hash(rawKey),
            Scope = string.IsNullOrWhiteSpace(scope) ? DefaultScope : scope.Trim(),
            ExpiresAt = expiresAt,
            RotatedFromKeyId = rotatedFromKeyId
        };
    }

    private async Task<ClientApiKey> GetOwnedApiKeyAsync(
        string clientKey,
        Guid apiKeyId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientKey))
        {
            throw new ArgumentException(ApplicationResourceText.ClientKeyRequired, nameof(clientKey));
        }

        var apiKey = await apiKeyRepository.GetByIdAsync(apiKeyId, cancellationToken)
            ?? throw new InvalidOperationException(ApplicationResourceText.ApiKeyNotFound(apiKeyId));

        if (!string.Equals(apiKey.Client?.ClientKey, clientKey.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(ApplicationResourceText.ApiKeyClientMismatch);
        }

        return apiKey;
    }

    private static void ValidateClientRequest(CreateApiClientRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClientKey))
        {
            throw new ArgumentException(ApplicationResourceText.ClientKeyRequired, nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ArgumentException(ApplicationResourceText.ClientDisplayNameRequired, nameof(request));
        }
    }

    private static IReadOnlyCollection<string> NormalizeAllowedProfiles(IReadOnlyCollection<string>? profiles)
    {
        return profiles?
            .Where(profile => !string.IsNullOrWhiteSpace(profile))
            .Select(profile => profile.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static void EnsureKeyCanBeChanged(ClientApiKey apiKey)
    {
        if (apiKey.RevokedAt is not null)
        {
            throw new InvalidOperationException(ApplicationResourceText.ApiKeyAlreadyRevoked);
        }
    }
}
