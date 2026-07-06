using DeValpo.MediaIntelligence.Application;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplicationAndInfrastructure_RegisterProductAbstractionsWithoutMocksByDefault()
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
        provider.GetServices<IMediaAnalysisSkill>()
            .Should()
            .ContainSingle()
            .Which.SkillName.Should().Be("GoogleDocumentAiDocumentOcrSkill");
    }

    [Fact]
    public void AddApplicationAndInfrastructure_RegistersMockSkills_WhenExplicitlyEnabled()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Skills:EnableMockSkills"] = "true",
            ["AnalysisProfiles:EnableMockProfiles"] = "true"
        });

        var services = new ServiceCollection()
            .AddApplication()
            .AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();

        provider.GetServices<IMediaAnalysisSkill>().Should().HaveCount(9);
    }

    [Fact]
    public async Task SkillRegistryValidation_Throws_WhenMockProfilesAreEnabledWithoutMockSkills()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["AnalysisProfiles:EnableMockProfiles"] = "true",
            ["Skills:EnableMockSkills"] = "false"
        });

        var services = new ServiceCollection()
            .AddApplication()
            .AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var hostedService = provider.GetServices<IHostedService>()
            .Single(service => service.GetType().Name == "SkillRegistryValidationHostedService");

        var act = () => hostedService.StartAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*references unregistered skill*");
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:MediaIntelligence"] = "Host=localhost;Port=5435;Database=media_intelligence;Username=postgres;Password=postgres",
            ["Storage:Provider"] = "Local",
            ["Storage:LocalRootPath"] = "storage/test-media-assets",
            ["GoogleDocumentAi:Enabled"] = "false"
        };

        foreach (var item in overrides)
        {
            values[item.Key] = item.Value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
