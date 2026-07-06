using System.Security.Claims;
using System.Text.Json;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Api.Auth;

public sealed class RequestClientAccessService(IClientRepository clientRepository) : IRequestClientAccessService
{
    private const string ClientScope = "media-intelligence:client";
    private const string AdminScope = "media-intelligence:admin";
    private const string OperatorScope = "media-intelligence:operator";
    private const string LegacyAdminScope = "admin";
    private const string LegacyOperatorScope = "operator";

    /// <summary>
    /// Checks client ownership and allowed profile assignment for analysis submissions.
    /// </summary>
    public async Task<bool> CanSubmitAsync(
        ClaimsPrincipal user,
        string clientKey,
        string profileKey,
        CancellationToken cancellationToken)
    {
        if (HasOperationalAccess(user))
        {
            return true;
        }

        if (!HasScope(user, ClientScope) ||
            !ClientClaimMatches(user, clientKey) ||
            string.IsNullOrWhiteSpace(profileKey))
        {
            return false;
        }

        var client = await clientRepository.GetByKeyAsync(clientKey.Trim(), cancellationToken);
        return client is { IsActive: true } && IsProfileAllowed(client, profileKey);
    }

    /// <summary>
    /// Checks ownership for job/result operations.
    /// </summary>
    public bool CanAccessClientResource(ClaimsPrincipal user, string resourceClientKey)
    {
        return HasOperationalAccess(user) ||
            (HasScope(user, ClientScope) && ClientClaimMatches(user, resourceClientKey));
    }

    /// <summary>
    /// Checks whether the principal has operator or admin privileges.
    /// </summary>
    public bool HasOperationalAccess(ClaimsPrincipal user)
    {
        return HasScope(user, AdminScope) ||
            HasScope(user, OperatorScope) ||
            HasScope(user, LegacyAdminScope) ||
            HasScope(user, LegacyOperatorScope);
    }

    private static bool ClientClaimMatches(ClaimsPrincipal user, string clientKey)
    {
        var claimValue = user.FindFirst(AuthClaimTypes.ClientKey)?.Value;
        return !string.IsNullOrWhiteSpace(claimValue) &&
            !string.IsNullOrWhiteSpace(clientKey) &&
            string.Equals(claimValue, clientKey.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasScope(ClaimsPrincipal user, string scope)
    {
        return user.FindAll(AuthClaimTypes.Scope)
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(value => string.Equals(value, scope, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsProfileAllowed(Client client, string profileKey)
    {
        try
        {
            var profiles = JsonSerializer.Deserialize<string[]>(client.AllowedProfileKeysJson) ?? [];
            return profiles.Any(profile =>
                string.Equals(profile, profileKey.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
