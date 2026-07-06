using DeValpo.MediaIntelligence.Infrastructure.Options;
using Google.Cloud.DocumentAI.V1;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills.Documents;

public interface IGoogleDocumentAiClientFactory
{
    /// <summary>
    /// Creates a Document AI client using configured credentials or ADC.
    /// </summary>
    Task<DocumentProcessorServiceClient> CreateAsync(
        GoogleDocumentAiOptions options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Builds the fully qualified Document AI processor name.
    /// </summary>
    ProcessorName CreateProcessorName(GoogleDocumentAiOptions options);
}
