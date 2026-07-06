using DeValpo.MediaIntelligence.Contracts.Capabilities;
using System.Reflection;
using System.Resources;

namespace DeValpo.MediaIntelligence.Application.Capabilities;

public interface ICapabilityApplicationService
{
    Task<IReadOnlyCollection<CapabilityResponse>> ListAsync(CancellationToken cancellationToken);
}

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

    private static string GetRequiredResource(string name)
    {
        return Resources.GetString(name)
            ?? throw new InvalidOperationException($"Missing capability resource '{name}'.");
    }
}
