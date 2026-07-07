using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Auth;
using DeValpo.MediaIntelligence.Application.AnalysisJobs;
using DeValpo.MediaIntelligence.Application.Capabilities;
using DeValpo.MediaIntelligence.Application.HumanReview;
using DeValpo.MediaIntelligence.Application.MediaAnalysis;
using DeValpo.MediaIntelligence.Application.Operations;
using DeValpo.MediaIntelligence.Application.Profiles;
using DeValpo.MediaIntelligence.Application.Retention;
using DeValpo.MediaIntelligence.Application.Skills;
using DeValpo.MediaIntelligence.Application.Webhooks;
using DeValpo.MediaIntelligence.Application.Audit;
using Microsoft.Extensions.DependencyInjection;

namespace DeValpo.MediaIntelligence.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers application-layer services, profile resolution and orchestration dependencies.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<AnalysisProfileOptions>();
        services.AddSingleton<IApiKeySecretGenerator, ApiKeySecretGenerator>();
        services.AddSingleton<IApiKeyHasher, ApiKeyHasher>();
        services.AddScoped<IClientAccessApplicationService, ClientAccessApplicationService>();
        services.AddScoped<ICapabilityApplicationService, CapabilityApplicationService>();
        services.AddScoped<ISkillApplicationService, SkillApplicationService>();
        services.AddScoped<IProfileApplicationService, ProfileApplicationService>();
        services.AddScoped<IAnalysisJobApplicationService, AnalysisJobApplicationService>();
        services.AddScoped<IHumanReviewApplicationService, HumanReviewApplicationService>();
        services.AddScoped<IWebhookDeliveryApplicationService, WebhookDeliveryApplicationService>();
        services.AddScoped<IOperationalMetricsApplicationService, OperationalMetricsApplicationService>();
        services.AddScoped<IAuditApplicationService, AuditApplicationService>();
        services.AddScoped<IRetentionApplicationService, RetentionApplicationService>();
        services.AddScoped<IMediaAnalysisApplicationService, MediaAnalysisApplicationService>();
        services.AddSingleton<IAnalysisProfileResolver, StaticAnalysisProfileResolver>();
        services.AddScoped<IMediaAnalysisOrchestrator, MediaAnalysisOrchestrator>();

        return services;
    }
}
