using DeValpo.MediaIntelligence.Infrastructure.Options;
using Google.Cloud.DocumentAI.V1;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills.Documents;

public interface IGoogleDocumentAiClientFactory
{
    Task<DocumentProcessorServiceClient> CreateAsync(
        GoogleDocumentAiOptions options,
        CancellationToken cancellationToken);

    ProcessorName CreateProcessorName(GoogleDocumentAiOptions options);
}
