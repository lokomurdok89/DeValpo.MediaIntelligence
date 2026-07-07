using Amazon.Rekognition;
using Amazon.S3;
using DeValpo.MediaIntelligence.Infrastructure.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition;

public interface IAwsRekognitionClientFactory
{
    /// <summary>
    /// Creates an S3 client for the configured AWS region and credential source.
    /// </summary>
    IAmazonS3 CreateS3Client(AwsRekognitionOptions options);

    /// <summary>
    /// Creates a Rekognition client for the configured AWS region and credential source.
    /// </summary>
    IAmazonRekognition CreateRekognitionClient(AwsRekognitionOptions options);
}
