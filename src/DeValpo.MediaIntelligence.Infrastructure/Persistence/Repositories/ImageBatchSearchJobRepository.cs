using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.CloudAssets;
using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Repositories;

public sealed class ImageBatchSearchJobRepository(
    MediaIntelligenceDbContext dbContext,
    IOptions<CloudAssetOptions> cloudAssetOptions) : IImageBatchSearchJobRepository
{
    private const string ProfileKey = "image-batch-search-v1";

    /// <summary>
    /// Adds a new image batch search job to the DbContext without saving immediately.
    /// </summary>
    public async Task AddAsync(ImageBatchSearchJob job, CancellationToken cancellationToken)
    {
        await dbContext.ImageBatchSearchJobs.AddAsync(job, cancellationToken);
    }

    /// <summary>
    /// Loads the oldest pending image batch search job with uploaded image metadata.
    /// </summary>
    public Task<ImageBatchSearchJob?> GetNextPendingWithAssetsAsync(CancellationToken cancellationToken)
    {
        var maxConcurrentJobs = Math.Max(cloudAssetOptions.Value.MaxConcurrentJobsPerClient, 1);

        return dbContext.ImageBatchSearchJobs
            .AsNoTracking()
            .Include(job => job.Assets)
            .Where(job => job.ProfileKey == ProfileKey && job.Status == "Pending" && job.Assets.Any())
            .Where(job =>
                dbContext.VideoSearchJobs.Count(candidate =>
                    candidate.ClientKey == job.ClientKey &&
                    candidate.Status == "Running") +
                dbContext.ImageBatchSearchJobs.Count(candidate =>
                    candidate.ClientKey == job.ClientKey &&
                    candidate.Status == "Running") < maxConcurrentJobs)
            .OrderBy(job => job.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Claims a pending image batch search job with a direct update.
    /// </summary>
    public async Task<bool> MarkRunningAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var updated = await dbContext.ImageBatchSearchJobs
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
    /// Loads an image batch search job graph for API responses and worker processing.
    /// </summary>
    public Task<ImageBatchSearchJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return dbContext.ImageBatchSearchJobs
            .AsSplitQuery()
            .Include(job => job.Assets)
            .Include(job => job.Matches)
            .Include(job => job.Issues)
            .FirstOrDefaultAsync(job => job.Id == jobId && job.ProfileKey == ProfileKey, cancellationToken);
    }

    /// <summary>
    /// Resets interrupted running image batch search jobs after a process restart.
    /// </summary>
    public Task<int> ResetRunningAsync(CancellationToken cancellationToken)
    {
        return dbContext.ImageBatchSearchJobs
            .Where(job => job.ProfileKey == ProfileKey && job.Status == "Running")
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, "Pending")
                    .SetProperty(job => job.ErrorMessage, (string?)null),
                cancellationToken);
    }

    /// <summary>
    /// Persists a failed or retryable image batch processing transition.
    /// </summary>
    public Task SetProcessingFailureAsync(
        Guid jobId,
        string status,
        string errorMessage,
        DateTimeOffset? completedAt,
        CancellationToken cancellationToken)
    {
        return dbContext.ImageBatchSearchJobs
            .Where(job => job.Id == jobId && job.ProfileKey == ProfileKey)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, status)
                    .SetProperty(job => job.ErrorMessage, errorMessage)
                    .SetProperty(job => job.CompletedAt, completedAt),
                cancellationToken);
    }

    /// <summary>
    /// Saves image batch output and moves the job to its terminal success status.
    /// </summary>
    public async Task CompleteProcessingAsync(
        Guid jobId,
        string status,
        IReadOnlyCollection<ImageBatchSearchMatch> matches,
        IReadOnlyCollection<ImageBatchSearchIssue> issues,
        IReadOnlyDictionary<Guid, string> providerInputUris,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.ImageBatchSearchMatches.AddRangeAsync(matches, cancellationToken);
        await dbContext.ImageBatchSearchIssues.AddRangeAsync(issues, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var item in providerInputUris)
        {
            await dbContext.ImageBatchSearchAssets
                .Where(asset => asset.Id == item.Key)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(asset => asset.ProviderInputUri, item.Value),
                    cancellationToken);
        }

        await dbContext.ImageBatchSearchJobs
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
