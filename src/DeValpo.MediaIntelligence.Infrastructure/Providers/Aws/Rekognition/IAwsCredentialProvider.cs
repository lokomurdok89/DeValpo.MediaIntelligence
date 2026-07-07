using Amazon.Runtime;
using DeValpo.MediaIntelligence.Infrastructure.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition;

public interface IAwsCredentialProvider
{
    /// <summary>
    /// Resolves AWS credentials from a configured mounted credentials file or the AWS SDK chain.
    /// </summary>
    AWSCredentials GetCredentials(AwsRekognitionOptions options);

    /// <summary>
    /// Returns whether a local credential source appears to be present without exposing secret values.
    /// </summary>
    bool IsCredentialSourcePresent(AwsRekognitionOptions options);

    /// <summary>
    /// Returns whether the configured credentials file exists when a path is provided.
    /// </summary>
    bool IsCredentialFilePresent(AwsRekognitionOptions options);
}
