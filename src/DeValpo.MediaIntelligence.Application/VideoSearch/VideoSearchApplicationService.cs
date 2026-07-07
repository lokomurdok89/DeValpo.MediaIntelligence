using System.Text.Json;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Resources;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisResults;
using DeValpo.MediaIntelligence.Contracts.VideoSearch;
using DeValpo.MediaIntelligence.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Application.VideoSearch;

public sealed class VideoSearchApplicationService(
    IMediaAssetStorage storage,
    IAnalysisProfileResolver profileResolver,
    IVideoSearchJobRepository jobRepository,
    IVideoSearchProvider provider,
    IUnitOfWork unitOfWork,
    IOptions<VideoAnalysisOptions> options,
    ILogger<VideoSearchApplicationService> logger) : IVideoSearchApplicationService
{
    private const string ProfileKey = "event-video-target-search-v1";

    /// <summary>
    /// Stores all source videos and creates a pending video search job.
    /// </summary>
    public async Task<CreateVideoSearchJobResponse> EnqueueAsync(
        CreateVideoSearchJobCommand command,
        CancellationToken cancellationToken)
    {
        var manifest = await ValidateRequestAsync(command, cancellationToken);
        var job = new VideoSearchJob
        {
            ClientKey = command.ClientKey.Trim(),
            ProfileKey = command.ProfileKey.Trim(),
            Status = AnalysisJobStatus.Pending.ToString(),
            ManifestJson = JsonSerializer.Serialize(manifest)
        };

        await jobRepository.AddAsync(job, cancellationToken);

        var index = 1;
        foreach (var video in command.Videos)
        {
            var storedAsset = await storage.SaveAsync(
                video.Content,
                video.OriginalFileName,
                video.ContentType,
                cancellationToken);

            job.Assets.Add(new VideoSearchAsset
            {
                JobId = job.Id,
                AssetKey = $"video-{index}",
                AssetRole = "SourceVideo",
                OriginalFileName = video.OriginalFileName,
                StoragePath = storedAsset.StoragePath,
                ContentType = storedAsset.ContentType,
                SizeInBytes = storedAsset.SizeInBytes
            });
            index++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(ApplicationResourceText.VideoSearchJobQueuedLogMessage);
        return new CreateVideoSearchJobResponse(job.Id, job.Status, job.CreatedAt);
    }

    /// <summary>
    /// Claims and processes the next pending video search job.
    /// </summary>
    public async Task<bool> ProcessNextPendingAsync(CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetNextPendingWithAssetsAsync(cancellationToken);
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
        catch (Exception exception) when (attempt < options.Value.MaxProcessingAttempts)
        {
            logger.LogWarning(exception, ApplicationResourceText.VideoSearchJobProcessingRetryLogMessage);
            await jobRepository.SetProcessingFailureAsync(
                job.Id,
                AnalysisJobStatus.Pending.ToString(),
                exception.Message,
                completedAt: null,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, ApplicationResourceText.VideoSearchJobProcessingFailedLogMessage);
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
    /// Returns interrupted running video jobs to Pending.
    /// </summary>
    public Task<int> ResetRunningJobsAsync(CancellationToken cancellationToken)
    {
        return jobRepository.ResetRunningAsync(cancellationToken);
    }

    /// <summary>
    /// Returns job metadata and current processing state.
    /// </summary>
    public async Task<VideoSearchJobResponse?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        return job is null ? null : MapJob(job);
    }

    /// <summary>
    /// Maps the persisted video search graph to its API response shape.
    /// </summary>
    public async Task<VideoSearchResultResponse?> GetResultAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        return job is null ? null : MapResult(job);
    }

    private async Task ProcessJobAsync(VideoSearchJob job, CancellationToken cancellationToken)
    {
        var manifest = VideoSearchManifestParser.Parse(job.ManifestJson, options.Value);
        var providerResult = await provider.AnalyzeAsync(
            new VideoSearchProviderRequest(
                job.Id,
                job.Assets.Select(asset => new VideoSearchProviderAsset(
                    asset.Id,
                    asset.AssetKey,
                    asset.OriginalFileName,
                    asset.StoragePath,
                    asset.ContentType)).ToArray(),
                manifest.DetectFaces,
                manifest.DetectNumbers),
            cancellationToken);

        if (providerResult.VideoDurations.Values.Any(duration => duration > options.Value.MaxDurationSeconds))
        {
            throw new InvalidOperationException(ApplicationResourceText.VideoSearchMaxDurationExceeded(options.Value.MaxDurationSeconds));
        }

        var targetSegments = BuildTargetSegments(job.Id, manifest, providerResult.TextOccurrences);
        var faceSegments = providerResult.FaceOccurrences
            .Where(item => item.Confidence >= manifest.MinConfidence)
            .Select(item => new VideoSearchFaceOccurrenceSegment
            {
                JobId = job.Id,
                VideoAssetId = item.AssetId,
                VideoKey = item.VideoKey,
                VideoFileName = item.VideoFileName,
                StartTimeSeconds = item.StartTimeSeconds,
                EndTimeSeconds = item.EndTimeSeconds,
                Confidence = item.Confidence,
                SourceProvider = item.SourceProvider
            })
            .ToArray();

        var issues = providerResult.Issues.Select(item => new VideoSearchIssue
        {
            JobId = job.Id,
            Code = item.Code,
            Message = item.Message,
            Severity = item.Severity,
            SourceProvider = item.SourceProvider
        }).ToArray();

        await jobRepository.CompleteProcessingAsync(
            job.Id,
            AnalysisJobStatus.Completed.ToString(),
            targetSegments,
            faceSegments,
            issues,
            providerResult.ProviderInputUris,
            providerResult.VideoDurations,
            DateTimeOffset.UtcNow,
            cancellationToken);

        logger.LogInformation(ApplicationResourceText.VideoSearchJobProcessingCompletedLogMessage);
    }

    private async Task<VideoSearchManifest> ValidateRequestAsync(
        CreateVideoSearchJobCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ClientKey))
        {
            throw new ArgumentException(ApplicationResourceText.ClientKeyRequired, nameof(command));
        }

        if (!string.Equals(command.ProfileKey, ProfileKey, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(ApplicationResourceText.VideoSearchProfileRequired(ProfileKey), nameof(command));
        }

        if (command.Videos.Count == 0)
        {
            throw new ArgumentException(ApplicationResourceText.VideoSearchVideosRequired, nameof(command));
        }

        if (command.Videos.Count > options.Value.MaxVideosPerJob)
        {
            throw new ArgumentException(ApplicationResourceText.VideoSearchMaxVideosExceeded(options.Value.MaxVideosPerJob), nameof(command));
        }

        var profile = await profileResolver.ResolveAsync(command.ProfileKey, cancellationToken)
            ?? throw new InvalidOperationException(ApplicationResourceText.ProfileNotFound(command.ProfileKey));

        if (!profile.SupportedAssetTypes.Contains(MediaAssetType.Video))
        {
            throw new InvalidOperationException(ApplicationResourceText.ProfileAssetTypeUnsupported(command.ProfileKey, MediaAssetType.Video));
        }

        return VideoSearchManifestParser.Parse(command.ManifestJson, options.Value);
    }

    private static IReadOnlyCollection<VideoSearchTargetMatchSegment> BuildTargetSegments(
        Guid jobId,
        VideoSearchManifest manifest,
        IReadOnlyCollection<VideoTextOccurrence> textOccurrences)
    {
        if (!manifest.DetectNumbers)
        {
            return [];
        }

        var rawSegments = new List<VideoSearchTargetMatchSegment>();
        foreach (var target in manifest.Targets)
        {
            var normalizedTarget = VideoSearchTextNormalizer.NormalizeNumber(target.VisibleNumber);
            foreach (var occurrence in textOccurrences)
            {
                if (occurrence.Confidence < manifest.MinConfidence ||
                    !VideoSearchTextNormalizer.NormalizeNumber(occurrence.Text).Contains(normalizedTarget, StringComparison.Ordinal))
                {
                    continue;
                }

                rawSegments.Add(new VideoSearchTargetMatchSegment
                {
                    JobId = jobId,
                    VideoAssetId = occurrence.AssetId,
                    TargetKey = target.TargetKey,
                    VisibleNumber = target.VisibleNumber,
                    VideoKey = occurrence.VideoKey,
                    VideoFileName = occurrence.VideoFileName,
                    StartTimeSeconds = occurrence.StartTimeSeconds,
                    EndTimeSeconds = occurrence.EndTimeSeconds,
                    Confidence = occurrence.Confidence,
                    SourceProvider = occurrence.SourceProvider
                });
            }
        }

        return VideoSearchSegmentMerger.Merge(rawSegments, manifest.MergeGapSeconds);
    }

    private static VideoSearchResultResponse MapResult(VideoSearchJob job)
    {
        var targetResults = job.TargetMatchSegments
            .GroupBy(item => new { item.TargetKey, item.VisibleNumber })
            .Select(group => new VideoTargetResultResponse(
                group.Key.TargetKey,
                group.Key.VisibleNumber,
                group.OrderBy(item => item.VideoKey).ThenBy(item => item.StartTimeSeconds)
                    .Select(item => new VideoTargetMatchSegmentResponse(
                        item.TargetKey,
                        item.VisibleNumber,
                        item.VideoKey,
                        item.VideoFileName,
                        item.StartTimeSeconds,
                        item.EndTimeSeconds,
                        VideoTimecodeFormatter.Format(item.StartTimeSeconds),
                        VideoTimecodeFormatter.Format(item.EndTimeSeconds),
                        item.Confidence,
                        item.SourceProvider))
                    .ToArray()))
            .ToArray();

        return new VideoSearchResultResponse(
            job.Id,
            job.ClientKey,
            job.Status,
            targetResults,
            job.FaceOccurrenceSegments.OrderBy(item => item.VideoKey).ThenBy(item => item.StartTimeSeconds)
                .Select(item => new VideoFaceOccurrenceSegmentResponse(
                    item.VideoKey,
                    item.VideoFileName,
                    item.StartTimeSeconds,
                    item.EndTimeSeconds,
                    VideoTimecodeFormatter.Format(item.StartTimeSeconds),
                    VideoTimecodeFormatter.Format(item.EndTimeSeconds),
                    item.Confidence,
                    item.SourceProvider))
                .ToArray(),
            job.Issues.Select(item => new AnalysisIssueResponse(item.Code, item.Message, item.Severity, item.SourceProvider)).ToArray(),
            job.CreatedAt,
            job.CompletedAt);
    }

    private static VideoSearchJobResponse MapJob(VideoSearchJob job)
    {
        return new VideoSearchJobResponse(
            job.Id,
            job.ClientKey,
            job.ProfileKey,
            job.Status,
            job.ProcessingAttempts,
            job.Assets.Count,
            job.ErrorMessage,
            job.CreatedAt,
            job.CompletedAt);
    }

}
