using DeValpo.MediaIntelligence.Infrastructure.Options;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.DocumentAI.V1;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills.Documents;

internal sealed class GoogleDocumentAiClientFactory : IGoogleDocumentAiClientFactory
{
    public async Task<DocumentProcessorServiceClient> CreateAsync(
        GoogleDocumentAiOptions options,
        CancellationToken cancellationToken)
    {
        var builder = new DocumentProcessorServiceClientBuilder();
        GoogleCredential? credential = null;

        if (!string.IsNullOrWhiteSpace(options.CredentialsPath))
        {
            if (!File.Exists(options.CredentialsPath))
            {
                throw new FileNotFoundException("Google Document AI credentials file was not found.");
            }

            await using var stream = File.OpenRead(options.CredentialsPath);
#pragma warning disable CS0618
            credential = await GoogleCredential.FromStreamAsync(stream, cancellationToken);
#pragma warning restore CS0618
        }
        else
        {
            credential = await GoogleCredential.GetApplicationDefaultAsync(cancellationToken);
        }

        if (credential.IsCreateScopedRequired)
        {
            credential = credential.CreateScoped("https://www.googleapis.com/auth/cloud-platform");
        }

        builder.GoogleCredential = credential;

        return await builder.BuildAsync(cancellationToken);
    }

    public ProcessorName CreateProcessorName(GoogleDocumentAiOptions options)
    {
        return ProcessorName.FromProjectLocationProcessor(
            options.ProjectId,
            options.Location,
            options.ProcessorId);
    }
}
