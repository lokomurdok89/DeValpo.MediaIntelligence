using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Application.Resources;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisJobs;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.MediaAnalysis;

public sealed class MediaAnalysisApplicationService(
    IMediaAnalysisOrchestrator orchestrator,
    IMediaAssetStorage storage,
    IAnalysisJobRepository jobRepository,
    IUnitOfWork unitOfWork) : IMediaAnalysisApplicationService
{
    /// <summary>
    /// Creates a running job, stores the asset, executes orchestration and saves extracted output.
    /// </summary>
    public async Task<AnalysisResultResponse> AnalyzeAsync(
        CreateAnalysisJobRequest request,
        Stream content,
        CancellationToken cancellationToken)
    {
        var job = new AnalysisJob
        {
            ClientKey = request.ClientKey.Trim(),
            ProfileKey = request.ProfileKey.Trim(),
            AssetType = request.AssetType.ToString(),
            Status = AnalysisJobStatus.Running.ToString()
        };

        await jobRepository.AddAsync(job, cancellationToken);

        var storedAsset = await storage.SaveAsync(
            content,
            request.OriginalFileName ?? "asset",
            request.ContentType ?? "application/octet-stream",
            cancellationToken);

        job.MediaAssets.Add(new MediaAsset
        {
            JobId = job.Id,
            AssetType = request.AssetType.ToString(),
            OriginalFileName = request.OriginalFileName ?? storedAsset.FileName,
            StoragePath = storedAsset.StoragePath,
            ContentType = storedAsset.ContentType,
            SizeInBytes = storedAsset.SizeInBytes
        });

        await using var storedContent = await storage.OpenReadAsync(storedAsset.StoragePath, cancellationToken);
        var metadata = new Dictionary<string, string>(request.Metadata ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(request.ContentType))
        {
            metadata["contentType"] = request.ContentType;
        }

        var orchestrationResult = await orchestrator.AnalyzeAsync(
            new AnalysisOrchestrationRequest(
                job.Id,
                request.ClientKey,
                request.ProfileKey,
                request.AssetType,
                metadata),
            storedContent,
            cancellationToken);

        foreach (var skillResult in orchestrationResult.SkillResults)
        {
            job.SkillExecutions.Add(new SkillExecution
            {
                JobId = job.Id,
                SkillName = skillResult.SkillName,
                Status = "Completed",
                Confidence = skillResult.Confidence,
                CompletedAt = DateTimeOffset.UtcNow
            });
        }

        var result = new AnalysisResult
        {
            JobId = job.Id,
            Confidence = orchestrationResult.Confidence,
            RequiresHumanReview = orchestrationResult.RequiresHumanReview
        };

        foreach (var skillResult in orchestrationResult.SkillResults)
        {
            result.ExtractedFields.AddRange(skillResult.ExtractedFields.Select(item => new ExtractedField
            {
                Name = item.Name,
                Value = item.Value,
                Confidence = item.Confidence,
                SourceSkill = item.SourceSkill
            }));

            result.DetectedEntities.AddRange(skillResult.DetectedEntities.Select(item => new DetectedEntity
            {
                EntityType = item.EntityType,
                Value = item.Value,
                Confidence = item.Confidence,
                SourceSkill = item.SourceSkill
            }));

            result.DetectedLabels.AddRange(skillResult.DetectedLabels.Select(item => new DetectedLabel
            {
                Label = item.Label,
                Confidence = item.Confidence,
                SourceSkill = item.SourceSkill
            }));

            result.GeneratedClips.AddRange(skillResult.GeneratedClips.Select(item => new GeneratedClip
            {
                ClipKey = item.ClipKey,
                StoragePath = item.StoragePath,
                Confidence = item.Confidence,
                SourceSkill = item.SourceSkill
            }));
        }

        result.Issues.AddRange(orchestrationResult.Issues.Select(item => new AnalysisIssue
        {
            Code = item.Code,
            Message = item.Message,
            Severity = item.Severity,
            SourceSkill = item.SourceSkill
        }));

        job.AnalysisResult = result;
        job.Status = AnalysisJobStatus.Completed.ToString();
        job.CompletedAt = DateTimeOffset.UtcNow;

        if (orchestrationResult.RequiresHumanReview)
        {
            job.HumanReviewTasks.Add(new HumanReviewTask
            {
                JobId = job.Id,
                Reason = ApplicationResourceText.HumanReviewReason
            });
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MapResult(job.AnalysisResult);
    }

    /// <summary>
    /// Returns a completed analysis result for the requested job when one exists.
    /// </summary>
    public async Task<AnalysisResultResponse?> GetResultAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        return job?.AnalysisResult is null ? null : MapResult(job.AnalysisResult);
    }

    /// <summary>
    /// Converts the persisted analysis result graph to its API response model.
    /// </summary>
    private static AnalysisResultResponse MapResult(AnalysisResult result)
    {
        return new AnalysisResultResponse(
            result.JobId,
            result.Confidence,
            result.RequiresHumanReview,
            result.ExtractedFields.Select(item => new ExtractedFieldResponse(item.Name, item.Value, item.Confidence, item.SourceSkill)).ToArray(),
            result.DetectedEntities.Select(item => new DetectedEntityResponse(item.EntityType, item.Value, item.Confidence, item.SourceSkill)).ToArray(),
            result.DetectedLabels.Select(item => new DetectedLabelResponse(item.Label, item.Confidence, item.SourceSkill)).ToArray(),
            result.GeneratedClips.Select(item => new GeneratedClipResponse(item.ClipKey, item.StoragePath, item.Confidence, item.SourceSkill)).ToArray(),
            result.Issues.Select(item => new AnalysisIssueResponse(item.Code, item.Message, item.Severity, item.SourceSkill)).ToArray(),
            result.CreatedAt);
    }
}
