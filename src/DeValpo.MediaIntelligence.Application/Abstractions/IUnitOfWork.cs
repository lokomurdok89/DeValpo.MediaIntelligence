namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>
    /// Commits all pending persistence-context changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
