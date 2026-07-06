using System.Security.Cryptography;
using System.Text;

namespace DeValpo.MediaIntelligence.Application.Auth;

public sealed class ApiKeyHasher : IApiKeyHasher
{
    /// <summary>
    /// Hashes the raw API key using SHA-256 and returns a hex digest.
    /// </summary>
    public string Hash(string apiKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes);
    }
}
