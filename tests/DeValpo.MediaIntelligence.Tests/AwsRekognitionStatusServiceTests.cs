using Amazon.Rekognition;
using Amazon.Runtime;
using Amazon.S3;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class AwsRekognitionStatusServiceTests
{
    [Fact]
    public async Task GetStatusAsync_ReturnsDisabledWithoutCredentials()
    {
        var service = CreateService(new AwsRekognitionOptions
        {
            Enabled = false,
            FaceSearchEnabled = false
        });

        var status = await service.GetStatusAsync(checkRemote: false, CancellationToken.None);

        status.Enabled.Should().BeFalse();
        status.Configured.Should().BeFalse();
        status.MissingFields.Should().BeEmpty();
        status.Message.Should().Contain("AWS Rekognition");
        status.Message.Should().Contain("disabled");
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsMissingFields_WhenEnabledWithoutConfiguration()
    {
        var service = CreateService(new AwsRekognitionOptions
        {
            Enabled = true,
            FaceSearchEnabled = true
        });

        var status = await service.GetStatusAsync(checkRemote: false, CancellationToken.None);

        status.Configured.Should().BeFalse();
        status.MissingFields.Should().Contain([nameof(AwsRekognitionOptions.Region), nameof(AwsRekognitionOptions.BucketName), nameof(AwsRekognitionOptions.CollectionId)]);
        status.Message.Should().Contain("Missing AWS Rekognition configuration");
    }

    [Fact]
    public async Task GetStatusAsync_DoesNotCallAws_WhenRemoteCheckIsFalse()
    {
        var service = CreateService(new AwsRekognitionOptions
        {
            Enabled = true,
            FaceSearchEnabled = true,
            Region = "us-east-1",
            BucketName = "bucket-that-is-not-contacted",
            CollectionId = "collection-that-is-not-contacted"
        });

        var status = await service.GetStatusAsync(checkRemote: false, CancellationToken.None);

        status.Configured.Should().BeTrue();
        status.Message.Should().Contain("local configuration is complete");
    }

    private static AwsRekognitionStatusService CreateService(AwsRekognitionOptions options)
    {
        return new AwsRekognitionStatusService(
            Options.Create(options),
            new TestAwsCredentialProvider(),
            new TestAwsRekognitionClientFactory());
    }

    private sealed class TestAwsCredentialProvider : IAwsCredentialProvider
    {
        public AWSCredentials GetCredentials(AwsRekognitionOptions options)
        {
            return new AnonymousAWSCredentials();
        }

        public bool IsCredentialSourcePresent(AwsRekognitionOptions options)
        {
            return false;
        }

        public bool IsCredentialFilePresent(AwsRekognitionOptions options)
        {
            return false;
        }
    }

    private sealed class TestAwsRekognitionClientFactory : IAwsRekognitionClientFactory
    {
        public IAmazonS3 CreateS3Client(AwsRekognitionOptions options)
        {
            throw new InvalidOperationException("Remote AWS checks are not expected in this test.");
        }

        public IAmazonRekognition CreateRekognitionClient(AwsRekognitionOptions options)
        {
            throw new InvalidOperationException("Remote AWS checks are not expected in this test.");
        }
    }
}
