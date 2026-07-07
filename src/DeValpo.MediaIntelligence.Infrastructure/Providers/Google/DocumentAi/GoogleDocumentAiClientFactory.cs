using DeValpo.MediaIntelligence.Infrastructure.Options;
using Google.Cloud.DocumentAI.V1;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Google.DocumentAi;

internal sealed class GoogleDocumentAiClientFactory(
    IGoogleCredentialProvider credentialProvider) : IGoogleDocumentAiClientFactory
{
    /// <summary>
    /// Creates a Google Document AI client from a credentials file or Application Default Credentials.
    /// </summary>
    public async Task<DocumentProcessorServiceClient> CreateAsync(
        GoogleDocumentAiOptions options,
        CancellationToken cancellationToken)
    {
        var credential = await credentialProvider.GetCredentialAsync(options, cancellationToken);
        var builder = new DocumentProcessorServiceClientBuilder
        {
            GoogleCredential = credential
        };

        return await builder.BuildAsync(cancellationToken);
    }

    /// <summary>
    /// Converts project, location and processor id settings into the SDK processor identifier.
    /// </summary>
    public ProcessorName CreateProcessorName(GoogleDocumentAiOptions options)
    {
        return ProcessorName.FromProjectLocationProcessor(
            options.ProjectId,
            options.Location,
            options.ProcessorId);
    }
}
