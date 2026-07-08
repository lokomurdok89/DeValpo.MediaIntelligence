using System.Text.Json;
using DeValpo.MediaIntelligence.Application.Resources;
using DeValpo.MediaIntelligence.Application.VideoSearch;

namespace DeValpo.MediaIntelligence.Application.ImageBatchSearch;

internal static class ImageBatchSearchManifestParser
{
    private static readonly HashSet<string> AllowedRootProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "targets", "providers"
    };

    private static readonly HashSet<string> AllowedTargetProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "targetKey", "queryType", "visibleNumber", "referenceImageKey", "minConfidence", "minSimilarity"
    };

    private static readonly HashSet<string> PersonalDataPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "fullName", "email", "rut", "dni", "phone", "telephone", "documentNumber", "personId", "userId"
    };

    /// <summary>
    /// Parses the v1 manifest and rejects unsupported or personally identifying fields.
    /// </summary>
    public static ImageBatchSearchManifest Parse(string manifestJson, ImageBatchSearchOptions options)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
        {
            throw new ArgumentException(ApplicationResourceText.ImageBatchSearchManifestRequired);
        }

        using var document = JsonDocument.Parse(manifestJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(ApplicationResourceText.ImageBatchSearchManifestMustBeObject);
        }

        RejectUnexpectedProperties(document.RootElement, AllowedRootProperties);

        if (!TryGetProperty(document.RootElement, "targets", out var targetsElement) ||
            targetsElement.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException(ApplicationResourceText.ImageBatchSearchTargetsRequired);
        }

        var targets = new List<ImageBatchSearchTarget>();
        foreach (var targetElement in targetsElement.EnumerateArray())
        {
            targets.Add(ParseTarget(targetElement, options));
        }

        if (targets.Count == 0)
        {
            throw new ArgumentException(ApplicationResourceText.ImageBatchSearchTargetsRequired);
        }

        if (targets.Count > options.MaxTargetsPerJob)
        {
            throw new ArgumentException(ApplicationResourceText.ImageBatchSearchMaxTargetsExceeded(options.MaxTargetsPerJob));
        }

        return new ImageBatchSearchManifest(targets, ParseProviders(document.RootElement));
    }

    private static ImageBatchSearchTarget ParseTarget(JsonElement targetElement, ImageBatchSearchOptions options)
    {
        if (targetElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(ApplicationResourceText.ImageBatchSearchTargetMustBeObject);
        }

        RejectUnexpectedProperties(targetElement, AllowedTargetProperties);

        var targetKey = GetRequiredString(targetElement, "targetKey", ApplicationResourceText.ImageBatchSearchTargetKeyRequired);
        var queryType = GetRequiredString(targetElement, "queryType", ApplicationResourceText.ImageBatchSearchQueryTypeRequired);
        var minConfidence = GetOptionalDecimal(targetElement, "minConfidence") ?? options.DefaultMinConfidence;
        var minSimilarity = GetOptionalDecimal(targetElement, "minSimilarity") ?? options.DefaultMinSimilarity;

        if (minConfidence is < 0m or > 1m)
        {
            throw new ArgumentException(ApplicationResourceText.ImageBatchSearchMinConfidenceInvalid);
        }

        if (minSimilarity is < 0m or > 100m)
        {
            throw new ArgumentException(ApplicationResourceText.ImageBatchSearchMinSimilarityInvalid);
        }

        if (string.Equals(queryType, ImageBatchSearchQueryTypes.VisibleNumber, StringComparison.OrdinalIgnoreCase))
        {
            var visibleNumber = GetRequiredString(targetElement, "visibleNumber", ApplicationResourceText.ImageBatchSearchVisibleNumberRequired);
            if (VideoSearchTextNormalizer.NormalizeNumber(visibleNumber).Length == 0)
            {
                throw new ArgumentException(ApplicationResourceText.ImageBatchSearchVisibleNumberMustContainDigit);
            }

            return new ImageBatchSearchTarget(
                targetKey,
                ImageBatchSearchQueryTypes.VisibleNumber,
                visibleNumber,
                null,
                minConfidence,
                minSimilarity);
        }

        if (string.Equals(queryType, ImageBatchSearchQueryTypes.FaceReference, StringComparison.OrdinalIgnoreCase))
        {
            var referenceImageKey = GetRequiredString(targetElement, "referenceImageKey", ApplicationResourceText.ImageBatchSearchReferenceImageKeyRequired);
            return new ImageBatchSearchTarget(
                targetKey,
                ImageBatchSearchQueryTypes.FaceReference,
                null,
                referenceImageKey,
                minConfidence,
                minSimilarity);
        }

        throw new ArgumentException(ApplicationResourceText.ImageBatchSearchQueryTypeUnsupported(queryType));
    }

    private static IReadOnlyCollection<string> ParseProviders(JsonElement rootElement)
    {
        if (!TryGetProperty(rootElement, "providers", out var providersElement))
        {
            return [];
        }

        if (providersElement.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException(ApplicationResourceText.ImageBatchSearchProvidersMustBeArray);
        }

        return providersElement.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString()?.Trim() : null)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()!;
    }

    private static void RejectUnexpectedProperties(JsonElement element, HashSet<string> allowedProperties)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (PersonalDataPropertyNames.Contains(property.Name))
            {
                throw new ArgumentException(ApplicationResourceText.ImageBatchSearchPersonalDataNotAllowed(property.Name));
            }

            if (!allowedProperties.Contains(property.Name))
            {
                throw new ArgumentException(ApplicationResourceText.ImageBatchSearchManifestPropertyUnsupported(property.Name));
            }
        }
    }

    private static string GetRequiredString(JsonElement element, string propertyName, string message)
    {
        if (!TryGetProperty(element, propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new ArgumentException(message);
        }

        return property.GetString()!.Trim();
    }

    private static decimal? GetOptionalDecimal(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, propertyName, out var property)
            ? property.GetDecimal()
            : null;
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        foreach (var candidate in element.EnumerateObject())
        {
            if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                property = candidate.Value;
                return true;
            }
        }

        property = default;
        return false;
    }
}
