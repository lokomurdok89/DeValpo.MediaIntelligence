using DeValpo.MediaIntelligence.Infrastructure;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Google.DocumentAi;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class GoogleCredentialProviderTests
{
    [Fact]
    public async Task GetCredentialAsync_RejectsUserCredentialFile_WhenUserCredentialsAreNotAllowed()
    {
        var credentialsPath = await WriteCredentialFileAsync("{\"type\":\"authorized_user\"}");
        using var provider = CreateProvider(credentialsPath);
        var credentialProvider = provider.GetRequiredService<IGoogleCredentialProvider>();

        var act = () => credentialProvider.GetCredentialAsync(
            new GoogleDocumentAiOptions
            {
                CredentialsPath = credentialsPath,
                AllowUserCredentials = false
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*user ADC credentials are not allowed*");
    }

    [Fact]
    public async Task GetCredentialAsync_RejectsUnknownCredentialType()
    {
        var credentialsPath = await WriteCredentialFileAsync("{\"type\":\"unknown\"}");
        using var provider = CreateProvider(credentialsPath);
        var credentialProvider = provider.GetRequiredService<IGoogleCredentialProvider>();

        var act = () => credentialProvider.GetCredentialAsync(
            new GoogleDocumentAiOptions
            {
                CredentialsPath = credentialsPath,
                AllowUserCredentials = true
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*credentials are invalid*");
    }

    private static ServiceProvider CreateProvider(string credentialsPath)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MediaIntelligence"] = "Host=localhost;Port=5435;Database=media_intelligence;Username=postgres;Password=postgres",
                ["Storage:Provider"] = "Local",
                ["Storage:LocalRootPath"] = "storage/test-media-assets",
                ["GoogleDocumentAi:Enabled"] = "true",
                ["GoogleDocumentAi:ProjectId"] = "project",
                ["GoogleDocumentAi:Location"] = "us",
                ["GoogleDocumentAi:ProcessorId"] = "processor",
                ["GoogleDocumentAi:CredentialsPath"] = credentialsPath
            })
            .Build();

        return new ServiceCollection()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }

    private static async Task<string> WriteCredentialFileAsync(string json)
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, "google.json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }
}
