using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Resources;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisJobs;
using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.AnalysisJobs;

public sealed class AnalysisJobApplicationService(
    IAnalysisProfileResolver profileResolver,
    IAnalysisJobRepository jobRepository,
    IUnitOfWork unitOfWork) : IAnalysisJobApplicationService
{
    /// <summary>
    /// Validates the request, resolves the profile and persists a new pending job.
    /// </summary>
    public async Task<CreateAnalysisJobResponse> CreateAsync(
        CreateAnalysisJobRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var profile = await profileResolver.ResolveAsync(request.ProfileKey, cancellationToken)
            ?? throw new InvalidOperationException(ApplicationResourceText.ProfileNotFound(request.ProfileKey));

        if (!profile.SupportedAssetTypes.Contains(request.AssetType))
        {
            throw new InvalidOperationException(ApplicationResourceText.ProfileAssetTypeUnsupported(request.ProfileKey, request.AssetType));
        }

        var job = new AnalysisJob
        {
            ClientKey = request.ClientKey.Trim(),
            ProfileKey = request.ProfileKey.Trim(),
            AssetType = request.AssetType.ToString(),
            Status = AnalysisJobStatus.Pending.ToString()
        };

        await jobRepository.AddAsync(job, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateAnalysisJobResponse(job.Id, job.Status, job.ProcessingAttempts, job.CreatedAt);
    }

    /// <summary>
    /// Retrieves a job and converts it to the API response contract.
    /// </summary>
    public async Task<AnalysisJobResponse?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        return job is null ? null : Map(job);
    }

    /// <summary>
    /// Marks a non-terminal job as cancelled and records its completion timestamp.
    /// </summary>
    public async Task CancelAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken)
            ?? throw new InvalidOperationException(ApplicationResourceText.JobNotFound(jobId));

        if (job.Status is "Completed" or "Failed")
        {
            throw new InvalidOperationException(ApplicationResourceText.JobCannotBeCancelled(jobId, job.Status));
        }

        job.Status = AnalysisJobStatus.Cancelled.ToString();
        job.CompletedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Converts a persisted job entity to the public job response model.
    /// </summary>
    internal static AnalysisJobResponse Map(AnalysisJob job)
    {
        Enum.TryParse<MediaAssetType>(job.AssetType, out var assetType);
        Enum.TryParse<AnalysisJobStatus>(job.Status, out var status);

        return new AnalysisJobResponse(
            job.Id,
            job.ClientKey,
            job.ProfileKey,
            assetType,
            status,
            job.ProcessingAttempts,
            job.CreatedAt,
            job.CompletedAt,
            job.ErrorMessage);
    }

    /// <summary>
    /// Ensures the minimum required fields are present before a job is created.
    /// </summary>
    private static void ValidateRequest(CreateAnalysisJobRequest request)
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
    }
}
