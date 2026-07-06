namespace DeValpo.MediaIntelligence.Api.Options;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public bool RequireHttpsMetadata { get; init; } = true;
    public int AccessTokenMinutes { get; init; } = 60;
}
