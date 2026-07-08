using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.CloudAssets;
using DeValpo.MediaIntelligence.Application.FaceSearch;
using DeValpo.MediaIntelligence.Application.ImageBatchSearch;
using DeValpo.MediaIntelligence.Application.Profiles;
using DeValpo.MediaIntelligence.Application.VideoSearch;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.CloudAssets;
using DeValpo.MediaIntelligence.Infrastructure.Persistence;
using DeValpo.MediaIntelligence.Infrastructure.Persistence.Repositories;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Google.DocumentAi;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Google.Vision;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Google.VideoIntelligence;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using DeValpo.MediaIntelligence.Infrastructure.Skills;
using DeValpo.MediaIntelligence.Infrastructure.Storage;
using DeValpo.MediaIntelligence.Infrastructure.Webhooks;
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
        services.Configure<GoogleVisionOptions>(configuration.GetSection("GoogleVision"));
        services.Configure<GoogleVideoIntelligenceOptions>(configuration.GetSection("GoogleVideoIntelligence"));
        services.Configure<AwsRekognitionOptions>(configuration.GetSection("AwsRekognition"));
        services.Configure<CloudAssetOptions>(configuration.GetSection("CloudAssets"));
        services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
        services.Configure<SkillRegistrationOptions>(configuration.GetSection("Skills"));
        services.Configure<AnalysisProfileOptions>(configuration.GetSection("AnalysisProfiles"));
        services.Configure<WebhookDeliveryOptions>(configuration.GetSection("WebhookDelivery"));
        services.Configure<GoogleDocumentAiStatusCacheOptions>(configuration.GetSection("GoogleDocumentAiStatusCache"));
        services.AddMemoryCache();

        var connectionString = configuration.GetConnectionString("MediaIntelligence")
            ?? throw new InvalidOperationException(InfrastructureResourceText.ConnectionStringRequired);

        services.AddDbContext<MediaIntelligenceDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IAnalysisJobRepository, AnalysisJobRepository>();
        services.AddScoped<IAnalysisProfileRepository, AnalysisProfileRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IClientApiKeyRepository, ClientApiKeyRepository>();
        services.AddScoped<IHumanReviewTaskRepository, HumanReviewTaskRepository>();
        services.AddScoped<IClientWebhookTargetRepository, ClientWebhookTargetRepository>();
        services.AddScoped<IWebhookDeliveryRepository, WebhookDeliveryRepository>();
        services.AddScoped<IOperationalMetricsRepository, OperationalMetricsRepository>();
        services.AddScoped<IAuditEventRepository, AuditEventRepository>();
        services.AddScoped<IRetentionRepository, RetentionRepository>();
        services.AddScoped<IVideoSearchJobRepository, VideoSearchJobRepository>();
        services.AddScoped<IFaceSearchJobRepository, FaceSearchJobRepository>();
        services.AddScoped<IImageBatchSearchJobRepository, ImageBatchSearchJobRepository>();
        services.AddScoped<IWebhookSignatureService, WebhookSignatureService>();
        services.AddScoped<IWebhookDeliveryProcessor, WebhookDeliveryProcessor>();
        services.AddHttpClient<IWebhookDeliverySender, WebhookDeliverySender>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IMediaAssetStorage, LocalMediaAssetStorage>();
        services.AddScoped<ICloudAssetBrowser, S3AssetBrowser>();
        services.AddScoped<ICloudAssetBrowser, GcsAssetBrowser>();
        services.AddScoped<ICloudAssetReader, CloudAssetReader>();
        services.AddScoped<IGoogleCredentialProvider, GoogleCredentialProvider>();
        services.AddScoped<IGoogleDocumentAiClientFactory, GoogleDocumentAiClientFactory>();
        services.AddScoped<IGoogleDocumentAiStatusService, GoogleDocumentAiStatusService>();
        services.AddScoped<IGoogleVideoIntelligenceStatusService, GoogleVideoIntelligenceStatusService>();
        services.AddScoped<IGoogleVisionStatusService, GoogleVisionStatusService>();
        services.AddScoped<IAwsCredentialProvider, AwsCredentialProvider>();
        services.AddScoped<IAwsRekognitionClientFactory, AwsRekognitionClientFactory>();
        services.AddScoped<IAwsRekognitionStatusService, AwsRekognitionStatusService>();
        services.AddScoped<IFaceSearchProvider, AwsRekognitionFaceSearchProvider>();
        services.AddScoped<IImageBatchTextSearchProvider, AwsRekognitionImageBatchSearchProvider>();
        services.AddScoped<IImageBatchFaceSearchProvider, AwsRekognitionImageBatchSearchProvider>();
        services.AddScoped<IImageBatchTextSearchProvider, GoogleVisionImageBatchSearchProvider>();
        services.AddScoped<IImageBatchFaceSearchProvider, GoogleVisionImageBatchSearchProvider>();
        services.AddScoped<IVideoSearchProvider, GoogleVideoIntelligenceSearchProvider>();

        var skillRegistrationOptions = configuration.GetSection("Skills").Get<SkillRegistrationOptions>() ?? new SkillRegistrationOptions();

        if (skillRegistrationOptions.EnableMockSkills)
        {
            RegisterMockSkills(services);
        }

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

        services.AddOptions<GoogleVideoIntelligenceOptions>()
            .Bind(configuration.GetSection("GoogleVideoIntelligence"))
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.BucketName),
                InfrastructureResourceText.GoogleVideoIntelligenceRequiredOptions)
            .Validate(options => options.RemoteOperationTimeoutSeconds > 0,
                InfrastructureResourceText.GoogleVideoIntelligenceRemoteOperationTimeoutRequired)
            .ValidateOnStart();

        services.AddOptions<GoogleVisionOptions>()
            .Bind(configuration.GetSection("GoogleVision"))
            .ValidateOnStart();

        services.AddOptions<AwsRekognitionOptions>()
            .Bind(configuration.GetSection("AwsRekognition"))
            .Validate(options => !options.Enabled || !options.FaceSearchEnabled ||
                (!string.IsNullOrWhiteSpace(options.Region) &&
                 !string.IsNullOrWhiteSpace(options.BucketName) &&
                 !string.IsNullOrWhiteSpace(options.CollectionId) &&
                 options.MinSimilarity is > 0m and <= 100m &&
                 options.UsePolling),
                InfrastructureResourceText.MissingAwsRekognitionConfiguration("Region, BucketName, CollectionId, MinSimilarity, UsePolling"))
            .ValidateOnStart();

        services.AddOptions<CloudAssetOptions>()
            .Bind(configuration.GetSection("CloudAssets"))
            .Validate(options => options.MinStagingTtlDays > 0 &&
                options.DefaultStagingTtlDays >= options.MinStagingTtlDays &&
                options.DefaultStagingTtlDays <= options.MaxStagingTtlDays,
                "CloudAssets staging TTL must be between the configured min and max values.")
            .Validate(options => options.MaxAssetsPerJob > 0 &&
                options.MaxConcurrentJobsPerClient > 0 &&
                options.MaxConcurrentProviderCallsPerClient > 0,
                "CloudAssets limits must be greater than zero.")
            .ValidateOnStart();

        services.AddHostedService<SkillRegistryValidationHostedService>();

        return services;
    }

    private static void RegisterMockSkills(IServiceCollection services)
    {
        services.AddScoped<IMediaAnalysisSkill, MockDocumentOcrSkill>();
        services.AddScoped<IMediaAnalysisSkill, MockHandwrittenIdExtractionSkill>();
        services.AddScoped<IMediaAnalysisSkill, MockStampDetectionSkill>();
        services.AddScoped<IMediaAnalysisSkill, MockImageObjectDetectionSkill>();
        services.AddScoped<IMediaAnalysisSkill, MockSportsBibDetectionSkill>();
        services.AddScoped<IMediaAnalysisSkill, MockFaceDetectionSkill>();
        services.AddScoped<IMediaAnalysisSkill, MockVideoClipGenerationSkill>();
        services.AddScoped<IMediaAnalysisSkill, MockLiveVideoBufferAnalysisSkill>();
    }
}

internal sealed class SkillRegistryValidationHostedService(
    IServiceProvider serviceProvider,
    IOptions<SkillRegistrationOptions> skillRegistrationOptions) : Microsoft.Extensions.Hosting.IHostedService
{
    /// <summary>
    /// Validates at startup that every registered skill has a unique name.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var skills = scope.ServiceProvider.GetRequiredService<IEnumerable<IMediaAnalysisSkill>>();
        var profileResolver = scope.ServiceProvider.GetRequiredService<IAnalysisProfileResolver>();
        var duplicateName = skills
            .GroupBy(skill => skill.SkillName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;

        if (duplicateName is not null)
        {
            throw new InvalidOperationException(InfrastructureResourceText.DuplicateSkillName(duplicateName));
        }

        var skillsByName = skills.ToDictionary(skill => skill.SkillName, StringComparer.OrdinalIgnoreCase);
        var profiles = profileResolver.ListAsync(cancellationToken).GetAwaiter().GetResult();

        foreach (var profile in profiles)
        {
            foreach (var skillName in profile.SkillNames)
            {
                if (!skillsByName.ContainsKey(skillName))
                {
                    throw new InvalidOperationException(InfrastructureResourceText.ProfileReferencesUnregisteredSkill(profile.ProfileKey, skillName));
                }

                if (!skillRegistrationOptions.Value.EnableMockSkills &&
                    skillName.StartsWith("Mock", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(InfrastructureResourceText.ProductionProfileReferencesMockSkill(profile.ProfileKey, skillName));
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Completes immediately because the validation service does not own long-running work.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
