using System.Security.Cryptography;

namespace DeValpo.MediaIntelligence.Application.Auth;

public sealed class ApiKeySecretGenerator : IApiKeySecretGenerator
{
    /// <summary>
    /// Generates a random DeValpo MediaIntelligence API key.
    /// </summary>
    public string Generate()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);

        return $"dvmi_{Base64UrlEncode(bytes)}";
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
