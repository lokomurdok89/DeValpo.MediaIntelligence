using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Contracts.AnalysisJobs;
using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.AnalysisJobs;

public interface IAnalysisJobApplicationService
{
    Task<CreateAnalysisJobResponse> CreateAsync(CreateAnalysisJobRequest request, CancellationToken cancellationToken);
    Task<AnalysisJobResponse?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);
    Task CancelAsync(Guid jobId, CancellationToken cancellationToken);
}

public sealed class AnalysisJobApplicationService(
    IAnalysisProfileResolver profileResolver,
    IAnalysisJobRepository jobRepository,
    IUnitOfWork unitOfWork) : IAnalysisJobApplicationService
{
    public async Task<CreateAnalysisJobResponse> CreateAsync(
        CreateAnalysisJobRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var profile = await profileResolver.ResolveAsync(request.ProfileKey, cancellationToken)
            ?? throw new InvalidOperationException($"Profile '{request.ProfileKey}' was not found.");

        if (!profile.SupportedAssetTypes.Contains(request.AssetType))
        {
            throw new InvalidOperationException($"Profile '{request.ProfileKey}' does not support asset type '{request.AssetType}'.");
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

        return new CreateAnalysisJobResponse(job.Id, job.Status, job.CreatedAt);
    }

    public async Task<AnalysisJobResponse?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        return job is null ? null : Map(job);
    }

    public async Task CancelAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken)
            ?? throw new InvalidOperationException($"Job '{jobId}' was not found.");

        if (job.Status is "Completed" or "Failed")
        {
            throw new InvalidOperationException($"Job '{jobId}' cannot be cancelled from status '{job.Status}'.");
        }

        job.Status = AnalysisJobStatus.Cancelled.ToString();
        job.CompletedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

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
            job.CreatedAt,
            job.CompletedAt,
            job.ErrorMessage);
    }

    private static void ValidateRequest(CreateAnalysisJobRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClientKey))
        {
            throw new ArgumentException("ClientKey is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ProfileKey))
        {
            throw new ArgumentException("ProfileKey is required.", nameof(request));
        }

        if (request.AssetType == MediaAssetType.Unknown)
        {
            throw new ArgumentException("AssetType is required.", nameof(request));
        }
    }
}
