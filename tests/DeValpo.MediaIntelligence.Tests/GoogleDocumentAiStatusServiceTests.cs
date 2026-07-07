using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Google.DocumentAi;
using FluentAssertions;
using Google.Cloud.DocumentAI.V1;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class GoogleDocumentAiStatusServiceTests
{
    [Fact]
    public async Task GetStatusAsync_ReturnsNotConfigured_WhenRequiredConfigurationIsMissing()
    {
        var service = new GoogleDocumentAiStatusService(
            Options.Create(new GoogleDocumentAiOptions { Enabled = true }),
            new NoOpGoogleDocumentAiClientFactory());

        var status = await service.GetStatusAsync(checkRemote: false, CancellationToken.None);

        status.Configured.Should().BeFalse();
        status.MissingFields.Should().Contain([
            nameof(GoogleDocumentAiOptions.ProjectId),
            nameof(GoogleDocumentAiOptions.Location),
            nameof(GoogleDocumentAiOptions.ProcessorId)
        ]);
        status.MissingFields.Should().NotContain(nameof(GoogleDocumentAiOptions.CredentialsPath));
        status.Message.Should().Contain("Missing Google Document AI configuration");
    }

    [Fact]
    public async Task GetStatusAsync_AllowsApplicationDefaultCredentials_WhenCredentialPathIsEmpty()
    {
        var service = new GoogleDocumentAiStatusService(
            Options.Create(new GoogleDocumentAiOptions
            {
                Enabled = true,
                ProjectId = "test-project",
                Location = "us",
                ProcessorId = "processor"
            }),
            new NoOpGoogleDocumentAiClientFactory());

        var status = await service.GetStatusAsync(checkRemote: false, CancellationToken.None);

        status.Configured.Should().BeTrue();
        status.CredentialFilePresent.Should().BeFalse();
        status.MissingFields.Should().BeEmpty();
        status.Message.Should().Contain("Application Default Credentials");
    }

    [Fact]
    public async Task GetStatusAsync_DetectsMissingCredentialFile()
    {
        var service = new GoogleDocumentAiStatusService(
            Options.Create(new GoogleDocumentAiOptions
            {
                Enabled = true,
                ProjectId = "test-project",
                Location = "us",
                ProcessorId = "processor",
                CredentialsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "google.json")
            }),
            new NoOpGoogleDocumentAiClientFactory());

        var status = await service.GetStatusAsync(checkRemote: false, CancellationToken.None);

        status.Configured.Should().BeFalse();
        status.CredentialFilePresent.Should().BeFalse();
        status.MissingFields.Should().BeEmpty();
        status.Message.Should().Be("Google Document AI credentials file was not found.");
    }

    private sealed class NoOpGoogleDocumentAiClientFactory : IGoogleDocumentAiClientFactory
    {
        public Task<DocumentProcessorServiceClient> CreateAsync(
            GoogleDocumentAiOptions options,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public ProcessorName CreateProcessorName(GoogleDocumentAiOptions options)
        {
            return ProcessorName.FromProjectLocationProcessor("project", "us", "processor");
        }
    }
}
