using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using DeValpo.MediaIntelligence.Api.Auth;
using DeValpo.MediaIntelligence.Application.Audit;

namespace DeValpo.MediaIntelligence.Api.Audit;

internal static class AuditRequestFactory
{
    public static AuditEventRequest Create(
        ClaimsPrincipal user,
        string eventType,
        string? resourceType,
        string? resourceId,
        string? resourceClientKey,
        string outcome,
        IReadOnlyDictionary<string, string>? metadata = null,
        string? fallbackActorClientKey = null)
    {
        return new AuditEventRequest(
            eventType,
            GetSubject(user),
            user.FindFirst(AuthClaimTypes.ClientKey)?.Value ?? fallbackActorClientKey,
            resourceType,
            resourceId,
            resourceClientKey,
            outcome,
            metadata);
    }

    private static string? GetSubject(ClaimsPrincipal user)
    {
        return user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            user.Identity?.Name;
    }
}
