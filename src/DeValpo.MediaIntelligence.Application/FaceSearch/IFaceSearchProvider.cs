namespace DeValpo.MediaIntelligence.Application.FaceSearch;

public interface IFaceSearchProvider
{
    /// <summary>
    /// Reserved for the future provider that will compare consented reference images against video faces.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
}
