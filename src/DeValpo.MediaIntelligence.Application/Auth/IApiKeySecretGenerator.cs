namespace DeValpo.MediaIntelligence.Application.Auth;

public interface IApiKeySecretGenerator
{
    /// <summary>
    /// Generates a raw API key that must be shown only once.
    /// </summary>
    string Generate();
}
