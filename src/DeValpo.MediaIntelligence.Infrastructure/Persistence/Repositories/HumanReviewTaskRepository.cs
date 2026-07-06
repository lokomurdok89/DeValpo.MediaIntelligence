using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeValpo.MediaIntelligence.Infrastructure.Persistence.Repositories;

public sealed class HumanReviewTaskRepository(MediaIntelligenceDbContext dbContext) : IHumanReviewTaskRepository
{
    /// <summary>
    /// Lists review tasks with their owning job and analysis result graph.
    /// </summary>
    public async Task<IReadOnlyCollection<HumanReviewTask>> ListAsync(
        string? status,
        CancellationToken cancellationToken)
    {
        var query = BuildDetailsQuery();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(task => task.Status == status.Trim());
        }

        return await query
            .OrderBy(task => task.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Loads one review task with job, result and extracted fields.
    /// </summary>
    public Task<HumanReviewTask?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return BuildDetailsQuery().FirstOrDefaultAsync(task => task.Id == taskId, cancellationToken);
    }

    /// <summary>
    /// Stages replacement extracted fields in the current unit of work.
    /// </summary>
    public async Task ReplaceExtractedFieldsAsync(
        Guid analysisResultId,
        IReadOnlyCollection<ExtractedField> correctedFields,
        CancellationToken cancellationToken)
    {
        var existingFields = await dbContext.ExtractedFields
            .Where(field => field.AnalysisResultId == analysisResultId)
            .ToArrayAsync(cancellationToken);

        dbContext.ExtractedFields.RemoveRange(existingFields);
        await dbContext.ExtractedFields.AddRangeAsync(correctedFields, cancellationToken);
    }

    private IQueryable<HumanReviewTask> BuildDetailsQuery()
    {
        return dbContext.HumanReviewTasks
            .AsSplitQuery()
            .Include(task => task.Job)!.ThenInclude(job => job!.AnalysisResult)!.ThenInclude(result => result!.ExtractedFields);
    }
}
