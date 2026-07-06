using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;
using DeValpo.MediaIntelligence.Domain.Entities;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IMediaAnalysisSkill
{
    string SkillName { get; }
    IReadOnlyCollection<MediaAssetType> SupportedAssetTypes { get; }
    IReadOnlyCollection<string> RequiredCapabilities { get; }
    string Description { get; }

    Task<SkillExecutionResult> ExecuteAsync(
        SkillExecutionContext context,
        CancellationToken cancellationToken);
}

public interface IAnalysisProfileResolver
{
    Task<AnalysisProfileDefinition?> ResolveAsync(
        string profileKey,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AnalysisProfileDefinition>> ListAsync(
        CancellationToken cancellationToken);
}

public interface IMediaAnalysisOrchestrator
{
    Task<AnalysisOrchestrationResult> AnalyzeAsync(
        AnalysisOrchestrationRequest request,
        Stream content,
        CancellationToken cancellationToken);
}

public interface IMediaAssetStorage
{
    Task<StoredMediaAsset> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(
        string storagePath,
        CancellationToken cancellationToken);
}

public interface IAnalysisJobRepository
{
    Task AddAsync(AnalysisJob job, CancellationToken cancellationToken);
    Task<AnalysisJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);
}

public interface IAnalysisProfileRepository
{
    Task<IReadOnlyCollection<AnalysisProfile>> ListAsync(CancellationToken cancellationToken);
}

public interface IClientRepository
{
    Task<Client?> GetByKeyAsync(string clientKey, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
