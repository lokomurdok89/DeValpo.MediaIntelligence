namespace DeValpo.MediaIntelligence.Application.VideoSearch;

public interface IVideoSearchProvider
{
    /// <summary>
    /// Runs external video analysis and returns raw visible text and generic face occurrences.
    /// </summary>
    Task<VideoSearchProviderResult> AnalyzeAsync(
        VideoSearchProviderRequest request,
        CancellationToken cancellationToken);
}
