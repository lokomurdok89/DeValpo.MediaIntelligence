using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Persistence;
using DeValpo.MediaIntelligence.Infrastructure.Persistence.Repositories;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using DeValpo.MediaIntelligence.Infrastructure.Skills;
using DeValpo.MediaIntelligence.Infrastructure.Skills.Documents;
using DeValpo.MediaIntelligence.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure services, persistence, storage, skills and provider options.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        services.Configure<GoogleDocumentAiOptions>(configuration.GetSection("GoogleDocumentAi"));
        services.Configure<DatabaseOptions>(configuration.GetSection("Database"));

        var connectionString = configuration.GetConnectionString("MediaIntelligence")
            ?? throw new InvalidOperationException(InfrastructureResourceText.ConnectionStringRequired);

        services.AddDbContext<MediaIntelligenceDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IAnalysisJobRepository, AnalysisJobRepository>();
        services.AddScoped<IAnalysisProfileRepository, AnalysisProfileRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IMediaAssetStorage, LocalMediaAssetStorage>();
        services.AddScoped<IGoogleDocumentAiClientFactory, GoogleDocumentAiClientFactory>();
        services.AddScoped<IGoogleDocumentAiStatusService, GoogleDocumentAiStatusService>();

        services.AddScoped<IMediaAnalysisSkill, MockDocumentOcrSkill>();
        services.AddScoped<IMediaAnalysisSkill, MockHandwrittenIdExtractionSkill>();
        services.AddScoped<IMediaAnalysisSkill, MockStampDetectionSkill>();
        services.AddScoped<IMediaAnalysisSkill, MockImageObjectDetectionSkill>();
        services.AddScoped<IMediaAnalysisSkill, MockSportsBibDetectionSkill>();
        services.AddScoped<IMediaAnalysisSkill, MockFaceDetectionSkill>();
        services.AddScoped<IMediaAnalysisSkill, MockVideoClipGenerationSkill>();
        services.AddScoped<IMediaAnalysisSkill, MockLiveVideoBufferAnalysisSkill>();
        services.AddScoped<IMediaAnalysisSkill, GoogleDocumentAiDocumentOcrSkill>();

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection("Storage"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Provider), InfrastructureResourceText.StorageProviderRequired)
            .Validate(options => !string.IsNullOrWhiteSpace(options.LocalRootPath), InfrastructureResourceText.StorageLocalRootPathRequired)
            .ValidateOnStart();

        services.AddOptions<GoogleDocumentAiOptions>()
            .Bind(configuration.GetSection("GoogleDocumentAi"))
            .Validate(options => !options.Enabled ||
                (!string.IsNullOrWhiteSpace(options.ProjectId) &&
                 !string.IsNullOrWhiteSpace(options.Location) &&
                 !string.IsNullOrWhiteSpace(options.ProcessorId)),
                InfrastructureResourceText.GoogleDocumentAiRequiredOptions)
            .ValidateOnStart();

        services.AddHostedService<SkillRegistryValidationHostedService>();

        return services;
    }
}

internal sealed class SkillRegistryValidationHostedService(
    IServiceProvider serviceProvider) : Microsoft.Extensions.Hosting.IHostedService
{
    /// <summary>
    /// Validates at startup that every registered skill has a unique name.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var skills = scope.ServiceProvider.GetRequiredService<IEnumerable<IMediaAnalysisSkill>>();
        var duplicateName = skills
            .GroupBy(skill => skill.SkillName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;

        if (duplicateName is not null)
        {
            throw new InvalidOperationException(InfrastructureResourceText.DuplicateSkillName(duplicateName));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Completes immediately because the validation service does not own long-running work.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
