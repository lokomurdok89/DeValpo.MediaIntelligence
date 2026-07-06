using DeValpo.MediaIntelligence.Application;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplicationAndInfrastructure_RegisterApplicationAbstractions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MediaIntelligence"] = "Host=localhost;Port=5435;Database=media_intelligence;Username=postgres;Password=postgres",
                ["Storage:Provider"] = "Local",
                ["Storage:LocalRootPath"] = "storage/test-media-assets",
                ["GoogleDocumentAi:Enabled"] = "false"
            })
            .Build();

        var services = new ServiceCollection()
            .AddApplication()
            .AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IAnalysisProfileResolver>().Should().NotBeNull();
        provider.GetRequiredService<IMediaAnalysisOrchestrator>().Should().NotBeNull();
        provider.GetRequiredService<IMediaAssetStorage>().Should().NotBeNull();
        provider.GetRequiredService<IAnalysisJobRepository>().Should().NotBeNull();
        provider.GetRequiredService<IAnalysisProfileRepository>().Should().NotBeNull();
        provider.GetRequiredService<IClientRepository>().Should().NotBeNull();
        provider.GetRequiredService<IUnitOfWork>().Should().NotBeNull();
        provider.GetServices<IMediaAnalysisSkill>().Should().HaveCount(9);
    }
}
