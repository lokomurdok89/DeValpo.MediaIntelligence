namespace DeValpo.MediaIntelligence.Application.Auth;

public interface IApiKeyHasher
{
    /// <summary>
    /// Computes a deterministic hash for API key validation.
    /// </summary>
    string Hash(string apiKey);
}
