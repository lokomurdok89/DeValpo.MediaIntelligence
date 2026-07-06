using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Application.Resources;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisJobs;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Domain.Entities;
using DeValpo.MediaIntelligence.Application.Webhooks;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Application.MediaAnalysis;

public sealed class MediaAnalysisApplicationService(
    IMediaAnalysisOrchestrator orchestrator,
    IMediaAssetStorage storage,
    IAnalysisProfileResolver profileResolver,
    IAnalysisJobRepository jobRepository,
    IWebhookDeliveryApplicationService webhookDeliveryService,
    IUnitOfWork unitOfWork,
    IOptions<AnalysisProcessingOptions> processingOptions) : IMediaAnalysisApplicationService
{
    /// <summary>
    /// Stores the asset and creates a pending job for the background worker.
    /// </summary>
    public async Task<CreateAnalysisJobResponse> EnqueueAsync(
        CreateAnalysisJobRequest request,
        Stream content,
        CancellationToken cancellationToken)
    {
        await ValidateRequestAsync(request, cancellationToken);

        var job = new AnalysisJob
        {
            ClientKey = request.ClientKey.Trim(),
            ProfileKey = request.ProfileKey.Trim(),
            AssetType = request.AssetType.ToString(),
            Status = AnalysisJobStatus.Pending.ToString()
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

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateAnalysisJobResponse(job.Id, job.Status, job.ProcessingAttempts, job.CreatedAt);
    }

    /// <summary>
    /// Processes the next queued job with stored media, persisting every state transition.
    /// </summary>
    public async Task<bool> ProcessNextPendingAsync(CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetNextPendingWithAssetAsync(cancellationToken);
        if (job is null)
        {
            return false;
        }

        var attempt = job.ProcessingAttempts + 1;
        var claimed = await jobRepository.MarkRunningAsync(job.Id, cancellationToken);
        if (!claimed)
        {
            return true;
        }

        try
        {
            await ProcessJobAsync(job, cancellationToken);
        }
        catch (Exception exception) when (attempt < processingOptions.Value.MaxProcessingAttempts)
        {
            await jobRepository.SetProcessingFailureAsync(
                job.Id,
                AnalysisJobStatus.Pending.ToString(),
                exception.Message,
                completedAt: null,
                cancellationToken);
        }
        catch (Exception exception)
        {
            await jobRepository.SetProcessingFailureAsync(
                job.Id,
                AnalysisJobStatus.Failed.ToString(),
                exception.Message,
                DateTimeOffset.UtcNow,
                cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Returns interrupted running jobs to Pending so they can be retried.
    /// </summary>
    public Task<int> ResetRunningJobsAsync(CancellationToken cancellationToken)
    {
        return jobRepository.ResetRunningAsync(cancellationToken);
    }

    /// <summary>
    /// Returns a completed analysis result for the requested job when one exists.
    /// </summary>
    public async Task<AnalysisResultResponse?> GetResultAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        return job?.AnalysisResult is null ? null : MapResult(job.AnalysisResult);
    }

    private async Task ProcessJobAsync(AnalysisJob job, CancellationToken cancellationToken)
    {
        var asset = job.MediaAssets.OrderBy(item => item.CreatedAt).First();
        await using var storedContent = await storage.OpenReadAsync(asset.StoragePath, cancellationToken);

        Enum.TryParse<MediaAssetType>(job.AssetType, out var assetType);
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["contentType"] = asset.ContentType
        };

        var orchestrationResult = await orchestrator.AnalyzeAsync(
            new AnalysisOrchestrationRequest(
                job.Id,
                job.ClientKey,
                job.ProfileKey,
                assetType,
                metadata),
            storedContent,
            cancellationToken);

        var skillExecutions = new List<SkillExecution>();
        foreach (var skillResult in orchestrationResult.SkillResults)
        {
            skillExecutions.Add(new SkillExecution
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

        var status = orchestrationResult.RequiresHumanReview
            ? AnalysisJobStatus.RequiresReview.ToString()
            : AnalysisJobStatus.Completed.ToString();
        var completedAt = DateTimeOffset.UtcNow;
        var reviewTasks = new List<HumanReviewTask>();

        if (orchestrationResult.RequiresHumanReview)
        {
            reviewTasks.Add(new HumanReviewTask
            {
                JobId = job.Id,
                Reason = ApplicationResourceText.HumanReviewReason
            });
        }

        await jobRepository.CompleteProcessingAsync(
            job.Id,
            status,
            result,
            skillExecutions,
            reviewTasks,
            completedAt,
            cancellationToken);

        if (status == AnalysisJobStatus.Completed.ToString())
        {
            await webhookDeliveryService.EnqueueForJobAsync(job.Id, "analysis.completed", cancellationToken);
        }
    }

    private async Task ValidateRequestAsync(CreateAnalysisJobRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientKey))
        {
            throw new ArgumentException(ApplicationResourceText.ClientKeyRequired, nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ProfileKey))
        {
            throw new ArgumentException(ApplicationResourceText.ProfileKeyRequired, nameof(request));
        }

        if (request.AssetType == MediaAssetType.Unknown)
        {
            throw new ArgumentException(ApplicationResourceText.AssetTypeRequired, nameof(request));
        }

        var profile = await profileResolver.ResolveAsync(request.ProfileKey, cancellationToken)
            ?? throw new InvalidOperationException(ApplicationResourceText.ProfileNotFound(request.ProfileKey));

        if (!profile.SupportedAssetTypes.Contains(request.AssetType))
        {
            throw new InvalidOperationException(ApplicationResourceText.ProfileAssetTypeUnsupported(request.ProfileKey, request.AssetType));
        }
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
