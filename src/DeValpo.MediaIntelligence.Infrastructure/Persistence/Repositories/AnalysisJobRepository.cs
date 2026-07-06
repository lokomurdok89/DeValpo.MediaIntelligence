using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Repositories;

public sealed class AnalysisJobRepository(MediaIntelligenceDbContext dbContext) : IAnalysisJobRepository
{
    /// <summary>
    /// Adds a new job to the DbContext without saving immediately.
    /// </summary>
    public async Task AddAsync(AnalysisJob job, CancellationToken cancellationToken)
    {
        await dbContext.AnalysisJobs.AddAsync(job, cancellationToken);
    }

    /// <summary>
    /// Loads the oldest processable pending job and its stored asset metadata.
    /// </summary>
    public Task<AnalysisJob?> GetNextPendingWithAssetAsync(CancellationToken cancellationToken)
    {
        return dbContext.AnalysisJobs
            .AsNoTracking()
            .Include(job => job.MediaAssets)
            .Where(job => job.Status == "Pending" && job.MediaAssets.Any())
            .OrderBy(job => job.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Claims a pending job with a direct update so only one worker can process it.
    /// </summary>
    public async Task<bool> MarkRunningAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var updated = await dbContext.AnalysisJobs
            .Where(job => job.Id == jobId && job.Status == "Pending")
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, "Running")
                    .SetProperty(job => job.ErrorMessage, (string?)null)
                    .SetProperty(job => job.ProcessingAttempts, job => job.ProcessingAttempts + 1),
                cancellationToken);

        return updated == 1;
    }

    /// <summary>
    /// Loads a job and the related graph needed by API responses.
    /// </summary>
    public Task<AnalysisJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return dbContext.AnalysisJobs
            .AsSplitQuery()
            .Include(job => job.MediaAssets)
            .Include(job => job.SkillExecutions)
            .Include(job => job.HumanReviewTasks)
            .Include(job => job.AnalysisResult)!.ThenInclude(result => result!.ExtractedFields)
            .Include(job => job.AnalysisResult)!.ThenInclude(result => result!.DetectedEntities)
            .Include(job => job.AnalysisResult)!.ThenInclude(result => result!.DetectedLabels)
            .Include(job => job.AnalysisResult)!.ThenInclude(result => result!.GeneratedClips)
            .Include(job => job.AnalysisResult)!.ThenInclude(result => result!.Issues)
            .FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);
    }

    /// <summary>
    /// Returns interrupted running jobs to Pending so a restarted worker can retry them.
    /// </summary>
    public Task<int> ResetRunningAsync(CancellationToken cancellationToken)
    {
        return dbContext.AnalysisJobs
            .Where(job => job.Status == "Running")
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, "Pending")
                    .SetProperty(job => job.ErrorMessage, (string?)null),
                cancellationToken);
    }

    /// <summary>
    /// Updates failed/retry state directly so provider failures cannot leave a job stuck in Running.
    /// </summary>
    public Task SetProcessingFailureAsync(
        Guid jobId,
        string status,
        string errorMessage,
        DateTimeOffset? completedAt,
        CancellationToken cancellationToken)
    {
        return dbContext.AnalysisJobs
            .Where(job => job.Id == jobId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, status)
                    .SetProperty(job => job.ErrorMessage, errorMessage)
                    .SetProperty(job => job.CompletedAt, completedAt),
                cancellationToken);
    }

    /// <summary>
    /// Saves output rows and marks the job as completed or requiring review in one transaction.
    /// </summary>
    public async Task CompleteProcessingAsync(
        Guid jobId,
        string status,
        AnalysisResult result,
        IReadOnlyCollection<SkillExecution> skillExecutions,
        IReadOnlyCollection<HumanReviewTask> reviewTasks,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.AnalysisResults.AddAsync(result, cancellationToken);
        await dbContext.SkillExecutions.AddRangeAsync(skillExecutions, cancellationToken);
        await dbContext.HumanReviewTasks.AddRangeAsync(reviewTasks, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.AnalysisJobs
            .Where(job => job.Id == jobId && job.Status == "Running")
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, status)
                    .SetProperty(job => job.ErrorMessage, (string?)null)
                    .SetProperty(job => job.CompletedAt, completedAt),
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
