using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Application.CloudAssets;
using DeValpo.MediaIntelligence.Application.ImageBatchSearch;
using DeValpo.MediaIntelligence.Application.ImageBatchSearch.Models;
using DeValpo.MediaIntelligence.Application.Models;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Google.Vision;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class GoogleVisionImageBatchSearchProviderTests
{
    [Fact]
    public async Task SearchFacesAsync_ReturnsUnsupportedIssue_ForFaceReferenceTargets()
    {
        var provider = new GoogleVisionImageBatchSearchProvider(
            new FakeStorage(),
            new FakeCloudAssetReader(),
            Options.Create(new GoogleVisionOptions { Enabled = true }));

        var result = await provider.SearchFacesAsync(
            new ImageBatchFaceSearchRequest(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                [],
                [],
                [new ImageBatchSearchTarget("face-1", ImageBatchSearchQueryTypes.FaceReference, null, "reference-1", 0.8m, 90m)],
                90m),
            CancellationToken.None);

        result.Matches.Should().BeEmpty();
        result.Issues.Should().ContainSingle();
        result.Issues.Single().Code.Should().Be("GOOGLE_VISION_FACE_REFERENCE_UNSUPPORTED");
        result.Issues.Single().Severity.Should().Be("Unsupported");
        result.Issues.Single().TargetKey.Should().Be("face-1");
    }

    private sealed class FakeStorage : IMediaAssetStorage
    {
        public Task<StoredMediaAsset> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
        {
            return Task.FromResult(new StoredMediaAsset("stored/image.jpg", fileName, contentType, content.Length));
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream>(new MemoryStream([0xff, 0xd8, 0xff, 0xd9]));
        }

        public Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FakeCloudAssetReader : ICloudAssetReader
    {
        public Task<Stream> OpenReadAsync(
            string storageProvider,
            string bucketName,
            string objectKey,
            string? versionId,
            string? generation,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream>(new MemoryStream([0xff, 0xd8, 0xff, 0xd9]));
        }
    }
}
