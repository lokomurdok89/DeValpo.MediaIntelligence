namespace DeValpo.MediaIntelligence.Application.CloudAssets;

public interface IProviderRoutingPolicy
{
    /// <summary>
    /// Chooses the preferred processing provider for a profile, query type and source storage provider.
    /// </summary>
    string ResolveProvider(string profileKey, string? queryType, string? storageProvider);
}
