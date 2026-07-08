using DeValpo.MediaIntelligence.Application.ImageBatchSearch;

namespace DeValpo.MediaIntelligence.Application.CloudAssets;

public sealed class ProviderRoutingPolicy : IProviderRoutingPolicy
{
    /// <summary>
    /// Encodes provider routing decisions without leaking SDK concerns into application services.
    /// </summary>
    public string ResolveProvider(string profileKey, string? queryType, string? storageProvider)
    {
        if (string.Equals(profileKey, "event-face-search-v1", StringComparison.OrdinalIgnoreCase))
        {
            return "AwsRekognition";
        }

        if (string.Equals(profileKey, "image-batch-search-v1", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(queryType, ImageBatchSearchQueryTypes.FaceReference, StringComparison.OrdinalIgnoreCase))
        {
            return "AwsRekognition";
        }

        if (string.Equals(profileKey, "generic-document-ocr-v1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(profileKey, "invoice-extraction-v1", StringComparison.OrdinalIgnoreCase))
        {
            return "GoogleDocumentAi";
        }

        if (string.Equals(storageProvider, CloudStorageProviders.Gcs, StringComparison.OrdinalIgnoreCase))
        {
            return "GoogleVision";
        }

        return "AwsRekognition";
    }
}
