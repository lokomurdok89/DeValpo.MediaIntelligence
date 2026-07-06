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
    /// Loads a job and the related graph needed by API responses.
    /// </summary>
    public Task<AnalysisJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return dbContext.AnalysisJobs
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
}
