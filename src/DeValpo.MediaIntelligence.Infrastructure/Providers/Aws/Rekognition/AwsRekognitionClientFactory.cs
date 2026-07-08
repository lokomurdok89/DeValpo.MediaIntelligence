using Amazon;
using Amazon.Rekognition;
using Amazon.S3;
using DeValpo.MediaIntelligence.Infrastructure.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition;

internal sealed class AwsRekognitionClientFactory(
    IAwsCredentialProvider credentialProvider) : IAwsRekognitionClientFactory
{
    /// <summary>
    /// Creates an S3 client for staging temporary Rekognition input assets.
    /// </summary>
    public IAmazonS3 CreateS3Client(AwsRekognitionOptions options)
    {
        var region = RegionEndpoint.GetBySystemName(options.Region);
        return new AmazonS3Client(credentialProvider.GetCredentials(options), region);
    }

    /// <summary>
    /// Creates a Rekognition client for collection and video face-search calls.
    /// </summary>
    public IAmazonRekognition CreateRekognitionClient(AwsRekognitionOptions options)
    {
        var region = RegionEndpoint.GetBySystemName(options.Region);
        return new AmazonRekognitionClient(credentialProvider.GetCredentials(options), region);
    }
}
