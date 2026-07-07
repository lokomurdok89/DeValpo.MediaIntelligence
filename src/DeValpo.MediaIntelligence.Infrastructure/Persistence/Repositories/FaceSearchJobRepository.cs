using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Repositories;

public sealed class FaceSearchJobRepository(MediaIntelligenceDbContext dbContext) : IFaceSearchJobRepository
{
    private const string ProfileKey = "event-face-search-v1";

    /// <summary>
    /// Adds a new face search job to the DbContext without saving immediately.
    /// </summary>
    public async Task AddAsync(VideoSearchJob job, CancellationToken cancellationToken)
    {
        await dbContext.VideoSearchJobs.AddAsync(job, cancellationToken);
    }

    /// <summary>
    /// Loads the oldest pending face search job with uploaded video and reference image metadata.
    /// </summary>
    public Task<VideoSearchJob?> GetNextPendingWithAssetsAsync(CancellationToken cancellationToken)
    {
        return dbContext.VideoSearchJobs
            .AsNoTracking()
            .Include(job => job.Assets)
            .Where(job => job.ProfileKey == ProfileKey && job.Status == "Pending" && job.Assets.Any())
            .OrderBy(job => job.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Claims a pending face search job with a direct update.
    /// </summary>
    public async Task<bool> MarkRunningAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var updated = await dbContext.VideoSearchJobs
            .Where(job => job.Id == jobId && job.ProfileKey == ProfileKey && job.Status == "Pending")
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, "Running")
                    .SetProperty(job => job.ErrorMessage, (string?)null)
                    .SetProperty(job => job.ProcessingAttempts, job => job.ProcessingAttempts + 1),
                cancellationToken);

        return updated == 1;
    }

    /// <summary>
    /// Loads a face search job graph for API responses and worker processing.
    /// </summary>
    public Task<VideoSearchJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return dbContext.VideoSearchJobs
            .AsSplitQuery()
            .Include(job => job.Assets)
            .Include(job => job.FaceReferenceMatchSegments)
            .Include(job => job.Issues)
            .FirstOrDefaultAsync(job => job.Id == jobId && job.ProfileKey == ProfileKey, cancellationToken);
    }

    /// <summary>
    /// Resets interrupted running face search jobs after a process restart.
    /// </summary>
    public Task<int> ResetRunningAsync(CancellationToken cancellationToken)
    {
        return dbContext.VideoSearchJobs
            .Where(job => job.ProfileKey == ProfileKey && job.Status == "Running")
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, "Pending")
                    .SetProperty(job => job.ErrorMessage, (string?)null),
                cancellationToken);
    }

    /// <summary>
    /// Persists a failed or retryable processing transition.
    /// </summary>
    public Task SetProcessingFailureAsync(
        Guid jobId,
        string status,
        string errorMessage,
        DateTimeOffset? completedAt,
        CancellationToken cancellationToken)
    {
        return dbContext.VideoSearchJobs
            .Where(job => job.Id == jobId && job.ProfileKey == ProfileKey)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, status)
                    .SetProperty(job => job.ErrorMessage, errorMessage)
                    .SetProperty(job => job.CompletedAt, completedAt),
                cancellationToken);
    }

    /// <summary>
    /// Saves face search matches, provider URIs and terminal job status in one transaction.
    /// </summary>
    public async Task CompleteProcessingAsync(
        Guid jobId,
        string status,
        IReadOnlyCollection<VideoSearchFaceReferenceMatchSegment> faceMatches,
        IReadOnlyCollection<VideoSearchIssue> issues,
        IReadOnlyDictionary<Guid, string> providerInputUris,
        IReadOnlyDictionary<Guid, decimal> videoDurations,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.VideoSearchFaceReferenceMatchSegments.AddRangeAsync(faceMatches, cancellationToken);
        await dbContext.VideoSearchIssues.AddRangeAsync(issues, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var item in providerInputUris)
        {
            await dbContext.VideoSearchAssets
                .Where(asset => asset.Id == item.Key)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(asset => asset.ProviderInputUri, item.Value),
                    cancellationToken);
        }

        foreach (var item in videoDurations)
        {
            await dbContext.VideoSearchAssets
                .Where(asset => asset.Id == item.Key)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(asset => asset.VideoDurationSeconds, item.Value),
                    cancellationToken);
        }

        await dbContext.VideoSearchJobs
            .Where(job => job.Id == jobId && job.ProfileKey == ProfileKey && job.Status == "Running")
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, status)
                    .SetProperty(job => job.ErrorMessage, (string?)null)
                    .SetProperty(job => job.CompletedAt, completedAt),
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
