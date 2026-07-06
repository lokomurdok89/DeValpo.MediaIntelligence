using DeValpo.MediaIntelligence.Api.Resources;
using System.Reflection;
using System.Resources;

namespace DeValpo.MediaIntelligence.Api.Swagger;

internal static class SwaggerResourceText
{
    private static readonly ResourceManager Resources = new(
        "DeValpo.MediaIntelligence.Api.Resources.SwaggerResources",
        Assembly.GetExecutingAssembly());

    public static string DocumentName => GetRequiredResource(nameof(DocumentName));
    public static string Title => GetRequiredResource(nameof(Title));
    public static string Version => GetRequiredResource(nameof(Version));
    public static string Description => GetRequiredResource(nameof(Description));
    public static string EndpointName => GetRequiredResource(nameof(EndpointName));

    /// <summary>
    /// Reads a mandatory Swagger resource and fails during startup when it is missing.
    /// </summary>
    private static string GetRequiredResource(string name)
    {
        return Resources.GetString(name)
            ?? throw new InvalidOperationException(ApiResourceText.SwaggerResourceMissing(name));
    }
}
