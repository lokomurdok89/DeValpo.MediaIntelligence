using DeValpo.MediaIntelligence.Contracts.Capabilities;

namespace DeValpo.MediaIntelligence.Application.Capabilities;

public interface ICapabilityApplicationService
{
    Task<IReadOnlyCollection<CapabilityResponse>> ListAsync(CancellationToken cancellationToken);
}

public sealed class CapabilityApplicationService : ICapabilityApplicationService
{
    private static readonly CapabilityResponse[] Capabilities =
    [
        new("OCR", "OCR", "Extract visible text from documents and images."),
        new("HandwritingRecognition", "Handwriting recognition", "Extract handwritten identifiers or text."),
        new("StampDetection", "Stamp detection", "Detect administrative stamps and status marks."),
        new("ObjectDetection", "Object detection", "Detect objects or visual labels in images."),
        new("FaceDetection", "Face detection", "Structural support for future consent-based face detection."),
        new("PersonDetection", "Person detection", "Detect people in image or video assets."),
        new("BibNumberDetection", "Bib number detection", "Detect sports bib numbers."),
        new("VideoClipGeneration", "Video clip generation", "Generate structural clip metadata."),
        new("LiveVideoBuffering", "Live video buffering", "Analyze buffered live video windows."),
        new("DocumentExtraction", "Document extraction", "Extract structured document fields."),
        new("ExternalWebhookDelivery", "External webhook delivery", "Prepare future webhook delivery."),
        new("HumanReview", "Human review", "Create review tasks for low confidence results.")
    ];

    public Task<IReadOnlyCollection<CapabilityResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<CapabilityResponse>>(Capabilities);
    }
}
