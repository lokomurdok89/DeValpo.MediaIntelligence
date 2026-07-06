using DeValpo.MediaIntelligence.Contracts.Capabilities;
using DeValpo.MediaIntelligence.Application.Resources;
using System.Reflection;
using System.Resources;

namespace DeValpo.MediaIntelligence.Application.Capabilities;

public sealed class CapabilityApplicationService : ICapabilityApplicationService
{
    private static readonly ResourceManager Resources = new(
        "DeValpo.MediaIntelligence.Application.Resources.CapabilityResources",
        Assembly.GetExecutingAssembly());

    public Task<IReadOnlyCollection<CapabilityResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var capabilities = GetRequiredResource("CapabilityList")
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(capabilityKey => new CapabilityResponse(
                capabilityKey,
                GetRequiredResource($"{capabilityKey}_Name"),
                GetRequiredResource($"{capabilityKey}_Description")))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CapabilityResponse>>(capabilities);
    }

    /// <summary>
    /// Reads a mandatory localized capability resource and fails fast when it is missing.
    /// </summary>
    private static string GetRequiredResource(string name)
    {
        return Resources.GetString(name)
            ?? throw new InvalidOperationException(ApplicationResourceText.CapabilityResourceMissing(name));
    }
}
