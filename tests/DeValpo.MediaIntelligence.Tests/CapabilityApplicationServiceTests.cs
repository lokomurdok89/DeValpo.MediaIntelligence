using DeValpo.MediaIntelligence.Application.Capabilities;
using FluentAssertions;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class CapabilityApplicationServiceTests
{
    [Fact]
    public async Task ListAsync_LoadsCapabilitiesFromResources()
    {
        var service = new CapabilityApplicationService();

        var capabilities = await service.ListAsync(CancellationToken.None);

        capabilities.Should().HaveCount(12);
        capabilities.Should().Contain(capability =>
            capability.Key == "OCR" &&
            capability.Name == "OCR" &&
            !string.IsNullOrWhiteSpace(capability.Description));
        capabilities.Should().OnlyContain(capability =>
            !string.IsNullOrWhiteSpace(capability.Key) &&
            !string.IsNullOrWhiteSpace(capability.Name) &&
            !string.IsNullOrWhiteSpace(capability.Description));
    }
}
