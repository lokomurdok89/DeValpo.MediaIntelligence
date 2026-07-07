namespace DeValpo.MediaIntelligence.Application.FaceSearch;

public interface IFaceSearchApplicationService
{
    /// <summary>
    /// Reserved for the future AWS-backed face search workflow. No public job endpoint is exposed yet.
    /// </summary>
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken);
}
