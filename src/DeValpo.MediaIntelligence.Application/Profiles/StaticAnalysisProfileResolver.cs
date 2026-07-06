using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Application.Resources;
using DeValpo.MediaIntelligence.Contracts;

namespace DeValpo.MediaIntelligence.Application.Profiles;

public sealed class StaticAnalysisProfileResolver : IAnalysisProfileResolver
{
    // Constructor order for each profile:
    // profile key, client key, supported asset types, ordered skill names,
    // minimum confidence, human-review flag, human-readable description.
    private static readonly AnalysisProfileDefinition[] Profiles =
    [
        new(
            "arenysmed-delivery-evidence-v1",
            "arenysmed",
            [MediaAssetType.Document, MediaAssetType.Image],
            ["MockDocumentOcrSkill", "MockHandwrittenIdExtractionSkill", "MockStampDetectionSkill"],
            0.85m,
            true,
            ApplicationResourceText.ArenysmedDeliveryEvidenceDescription),
        new(
            "sports-event-clips-v1",
            "sports",
            [MediaAssetType.Image, MediaAssetType.Video, MediaAssetType.LiveVideo],
            ["MockSportsBibDetectionSkill", "MockImageObjectDetectionSkill", "MockVideoClipGenerationSkill"],
            0.75m,
            true,
            ApplicationResourceText.SportsEventClipsDescription),
        new(
            "generic-document-extraction-v1",
            "generic",
            [MediaAssetType.Document, MediaAssetType.Image],
            ["MockDocumentOcrSkill"],
            0.80m,
            true,
            ApplicationResourceText.GenericDocumentExtractionDescription),
        new(
            "invoice-extraction-v1",
            "generic",
            [MediaAssetType.Document],
            ["GoogleDocumentAiDocumentOcrSkill"],
            0.85m,
            true,
            ApplicationResourceText.InvoiceExtractionDescription)
    ];

    /// <summary>
    /// Finds a profile definition by its stable profile key.
    /// </summary>
    public Task<AnalysisProfileDefinition?> ResolveAsync(
        string profileKey,
        CancellationToken cancellationToken)
    {
        var profile = Profiles.FirstOrDefault(
            item => string.Equals(item.ProfileKey, profileKey, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(profile);
    }

    /// <summary>
    /// Returns all configured analysis profiles available in this deployment.
    /// </summary>
    public Task<IReadOnlyCollection<AnalysisProfileDefinition>> ListAsync(
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<AnalysisProfileDefinition>>(Profiles);
    }
}
