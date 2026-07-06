using System.Security.Claims;

namespace DeValpo.MediaIntelligence.Api.Auth;

public interface IRequestClientAccessService
{
    /// <summary>
    /// Checks whether the authenticated principal can submit work for a client/profile pair.
    /// </summary>
    Task<bool> CanSubmitAsync(
        ClaimsPrincipal user,
        string clientKey,
        string profileKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether the authenticated principal can read or mutate resources owned by the client.
    /// </summary>
    bool CanAccessClientResource(ClaimsPrincipal user, string resourceClientKey);

    /// <summary>
    /// Checks whether the authenticated principal can access operational diagnostics.
    /// </summary>
    bool HasOperationalAccess(ClaimsPrincipal user);
}
