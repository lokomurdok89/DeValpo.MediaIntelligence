using DeValpo.MediaIntelligence.Application.CloudAssets;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class CloudAssetReferenceTests
{
    [Fact]
    public void ParseOptional_AcceptsExplicitAssetsSelector()
    {
        var selector = CloudAssetSelectorParser.ParseOptional(
            """
            {
              "selectorMode": "ExplicitAssets",
              "storageProvider": "S3",
              "bucketName": "event-bucket",
              "region": "us-east-1",
              "assets": [
                {
                  "assetKey": "image-1",
                  "objectKey": "events/race/image-1.jpg",
                  "contentType": "image/jpeg",
                  "sizeInBytes": 123
                }
              ]
            }
            """);

        selector.Should().NotBeNull();
        selector!.SelectorMode.Should().Be(CloudAssetSelectorModes.ExplicitAssets);
        selector.StorageProvider.Should().Be(CloudStorageProviders.S3);
        selector.Assets.Should().ContainSingle(asset => asset.AssetKey == "image-1");
    }

    [Fact]
    public void ParseOptional_RejectsPublicUrls()
    {
        var act = () => CloudAssetSelectorParser.ParseOptional(
            """
            {
              "selectorMode": "ExplicitAssets",
              "storageProvider": "GCS",
              "bucketName": "docs-bucket",
              "assets": [
                {
                  "assetKey": "doc-1",
                  "objectKey": "docs/doc-1.pdf",
                  "signedUrl": "https://example.com/doc-1.pdf"
                }
              ]
            }
            """);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*URLs*");
    }

    [Fact]
    public async Task ResolveAsync_RejectsBucketOutsideAllowList()
    {
        var resolver = new AssetReferenceResolver(
            [new FakeBrowser()],
            Options.Create(new CloudAssetOptions { AllowedS3Buckets = ["allowed-bucket"] }));
        var selector = new CloudAssetSelector(
            CloudAssetSelectorModes.ExplicitAssets,
            CloudStorageProviders.S3,
            "denied-bucket",
            null,
            "us-east-1",
            null,
            [
                new CloudAssetReference(
                    "image-1",
                    CloudStorageProviders.S3,
                    "denied-bucket",
                    "image-1.jpg",
                    "us-east-1",
                    null,
                    "image/jpeg",
                    123,
                    null,
                    null,
                    null,
                    "image-1.jpg")
            ]);

        var act = () => resolver.ResolveAsync(selector, 10, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not allowed*");
    }

    private sealed class FakeBrowser : ICloudAssetBrowser
    {
        public string StorageProvider => CloudStorageProviders.S3;

        public Task<IReadOnlyCollection<ResolvedJobAsset>> ListByPrefixAsync(
            CloudAssetSelector selector,
            int maxAssets,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<ResolvedJobAsset>>([]);
        }

        public Task<IReadOnlyCollection<ResolvedJobAsset>> ResolveExplicitAsync(
            CloudAssetSelector selector,
            int maxAssets,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<ResolvedJobAsset>>([]);
        }
    }
}
