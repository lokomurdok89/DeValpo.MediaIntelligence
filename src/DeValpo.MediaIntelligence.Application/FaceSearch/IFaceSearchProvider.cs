using DeValpo.MediaIntelligence.Application.FaceSearch.Models;

namespace DeValpo.MediaIntelligence.Application.FaceSearch;

public interface IFaceSearchProvider
{
    /// <summary>
    /// Compares consented reference images against faces detected in source videos.
    /// </summary>
    Task<FaceSearchProviderResult> AnalyzeAsync(
        FaceSearchProviderRequest request,
        CancellationToken cancellationToken);
}
