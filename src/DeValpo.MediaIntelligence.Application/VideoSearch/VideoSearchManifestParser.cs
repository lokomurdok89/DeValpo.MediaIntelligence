using System.Text.Json;
using DeValpo.MediaIntelligence.Application.Resources;

namespace DeValpo.MediaIntelligence.Application.VideoSearch;

internal static class VideoSearchManifestParser
{
    private static readonly HashSet<string> AllowedRootProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "targets", "detectFaces", "detectNumbers", "minConfidence", "mergeGapSeconds"
    };

    private static readonly HashSet<string> AllowedTargetProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "targetKey", "visibleNumber"
    };

    private static readonly HashSet<string> PersonalDataPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "fullName", "email", "rut", "dni", "phone", "telephone", "documentNumber", "personId", "userId"
    };

    public static VideoSearchManifest Parse(string manifestJson, VideoAnalysisOptions options)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
        {
            throw new ArgumentException(ApplicationResourceText.VideoSearchManifestRequired);
        }

        using var document = JsonDocument.Parse(manifestJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(ApplicationResourceText.VideoSearchManifestMustBeObject);
        }

        RejectUnexpectedProperties(document.RootElement, AllowedRootProperties);

        var detectFaces = GetOptionalBoolean(document.RootElement, "detectFaces") ?? true;
        var detectNumbers = GetOptionalBoolean(document.RootElement, "detectNumbers") ?? true;
        if (!detectFaces && !detectNumbers)
        {
            throw new ArgumentException(ApplicationResourceText.VideoSearchAtLeastOneDetectionRequired);
        }

        var minConfidence = GetOptionalDecimal(document.RootElement, "minConfidence") ?? options.DefaultMinConfidence;
        if (minConfidence is < 0m or > 1m)
        {
            throw new ArgumentException(ApplicationResourceText.VideoSearchMinConfidenceInvalid);
        }

        var mergeGapSeconds = GetOptionalDecimal(document.RootElement, "mergeGapSeconds") ?? options.DefaultMergeGapSeconds;
        if (mergeGapSeconds is < 0m or > 60m)
        {
            throw new ArgumentException(ApplicationResourceText.VideoSearchMergeGapInvalid);
        }

        if (!TryGetProperty(document.RootElement, "targets", out var targetsElement) ||
            targetsElement.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException(ApplicationResourceText.VideoSearchTargetsRequired);
        }

        var targets = new List<VideoSearchTarget>();
        foreach (var targetElement in targetsElement.EnumerateArray())
        {
            if (targetElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException(ApplicationResourceText.VideoSearchTargetMustBeObject);
            }

            RejectUnexpectedProperties(targetElement, AllowedTargetProperties);

            var targetKey = GetRequiredString(targetElement, "targetKey", ApplicationResourceText.VideoSearchTargetKeyRequired);
            var visibleNumber = GetRequiredString(targetElement, "visibleNumber", ApplicationResourceText.VideoSearchVisibleNumberRequired);
            if (VideoSearchTextNormalizer.NormalizeNumber(visibleNumber).Length == 0)
            {
                throw new ArgumentException(ApplicationResourceText.VideoSearchVisibleNumberMustContainDigit);
            }

            targets.Add(new VideoSearchTarget(targetKey, visibleNumber));
        }

        if (targets.Count == 0)
        {
            throw new ArgumentException(ApplicationResourceText.VideoSearchTargetsRequired);
        }

        return new VideoSearchManifest(targets, detectFaces, detectNumbers, minConfidence, mergeGapSeconds);
    }

    private static void RejectUnexpectedProperties(JsonElement element, HashSet<string> allowedProperties)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (PersonalDataPropertyNames.Contains(property.Name))
            {
                throw new ArgumentException(ApplicationResourceText.VideoSearchPersonalDataNotAllowed(property.Name));
            }

            if (!allowedProperties.Contains(property.Name))
            {
                throw new ArgumentException(ApplicationResourceText.VideoSearchManifestPropertyUnsupported(property.Name));
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

    private static bool? GetOptionalBoolean(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, propertyName, out var property)
            ? property.GetBoolean()
            : null;
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
