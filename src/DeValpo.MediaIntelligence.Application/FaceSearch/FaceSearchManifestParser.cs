using System.Text.Json;
using DeValpo.MediaIntelligence.Application.Resources;

namespace DeValpo.MediaIntelligence.Application.FaceSearch;

internal static class FaceSearchManifestParser
{
    private static readonly HashSet<string> AllowedRootProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "targets", "minSimilarity"
    };

    private static readonly HashSet<string> AllowedTargetProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "targetKey", "referenceImageKey"
    };

    private static readonly HashSet<string> PersonalDataPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "fullName", "email", "rut", "dni", "phone", "telephone", "documentNumber", "personId", "userId"
    };

    /// <summary>
    /// Parses the face search manifest and rejects personal data fields.
    /// </summary>
    public static FaceSearchManifest Parse(string manifestJson, decimal defaultMinSimilarity)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
        {
            throw new ArgumentException(ApplicationResourceText.FaceSearchManifestRequired);
        }

        using var document = JsonDocument.Parse(manifestJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(ApplicationResourceText.FaceSearchManifestMustBeObject);
        }

        RejectUnexpectedProperties(document.RootElement, AllowedRootProperties);

        var minSimilarity = GetOptionalDecimal(document.RootElement, "minSimilarity") ?? defaultMinSimilarity;
        if (minSimilarity is < 0m or > 100m)
        {
            throw new ArgumentException(ApplicationResourceText.FaceSearchMinSimilarityInvalid);
        }

        if (!TryGetProperty(document.RootElement, "targets", out var targetsElement) ||
            targetsElement.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException(ApplicationResourceText.FaceSearchTargetsRequired);
        }

        var targets = new List<FaceSearchTarget>();
        foreach (var targetElement in targetsElement.EnumerateArray())
        {
            if (targetElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException(ApplicationResourceText.FaceSearchTargetMustBeObject);
            }

            RejectUnexpectedProperties(targetElement, AllowedTargetProperties);

            targets.Add(new FaceSearchTarget(
                GetRequiredString(targetElement, "targetKey", ApplicationResourceText.FaceSearchTargetKeyRequired),
                GetRequiredString(targetElement, "referenceImageKey", ApplicationResourceText.FaceSearchReferenceImageKeyRequired)));
        }

        if (targets.Count == 0)
        {
            throw new ArgumentException(ApplicationResourceText.FaceSearchTargetsRequired);
        }

        return new FaceSearchManifest(targets, minSimilarity);
    }

    private static void RejectUnexpectedProperties(JsonElement element, HashSet<string> allowedProperties)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (PersonalDataPropertyNames.Contains(property.Name))
            {
                throw new ArgumentException(ApplicationResourceText.FaceSearchPersonalDataNotAllowed(property.Name));
            }

            if (!allowedProperties.Contains(property.Name))
            {
                throw new ArgumentException(ApplicationResourceText.FaceSearchManifestPropertyUnsupported(property.Name));
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
