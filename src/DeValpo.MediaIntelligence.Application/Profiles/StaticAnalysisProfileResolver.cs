using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Contracts;

namespace DeValpo.MediaIntelligence.Application.Profiles;

public sealed class StaticAnalysisProfileResolver : IAnalysisProfileResolver
{
    private static readonly AnalysisProfileDefinition[] Profiles =
    [
        new(
            "arenysmed-delivery-evidence-v1",
            "arenysmed",
            [MediaAssetType.Document, MediaAssetType.Image],
            ["MockDocumentOcrSkill", "MockHandwrittenIdExtractionSkill", "MockStampDetectionSkill"],
            0.85m,
            true,
            "Administrative evidence profile for handwritten IDs, stamps and visible text."),
        new(
            "sports-event-clips-v1",
            "sports",
            [MediaAssetType.Image, MediaAssetType.Video, MediaAssetType.LiveVideo],
            ["MockSportsBibDetectionSkill", "MockImageObjectDetectionSkill", "MockVideoClipGenerationSkill"],
            0.75m,
            true,
            "Sports profile for bibs, objects and structural clip metadata."),
        new(
            "generic-document-extraction-v1",
            "generic",
            [MediaAssetType.Document, MediaAssetType.Image],
            ["MockDocumentOcrSkill"],
            0.80m,
            true,
            "Generic profile for document OCR and field extraction."),
        new(
            "invoice-extraction-v1",
            "generic",
            [MediaAssetType.Document],
            ["GoogleDocumentAiDocumentOcrSkill"],
            0.85m,
            true,
            "Document AI backed invoice/document extraction profile when Google is enabled.")
    ];

    public Task<AnalysisProfileDefinition?> ResolveAsync(
        string profileKey,
        CancellationToken cancellationToken)
    {
        var profile = Profiles.FirstOrDefault(
            item => string.Equals(item.ProfileKey, profileKey, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(profile);
    }

    public Task<IReadOnlyCollection<AnalysisProfileDefinition>> ListAsync(
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<AnalysisProfileDefinition>>(Profiles);
    }
}
