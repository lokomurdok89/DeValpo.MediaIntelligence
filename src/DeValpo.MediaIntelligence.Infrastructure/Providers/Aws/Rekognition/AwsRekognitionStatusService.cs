using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Providers;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Aws.Rekognition;

public sealed class AwsRekognitionStatusService(
    IOptions<AwsRekognitionOptions> options) : IAwsRekognitionStatusService
{
    /// <summary>
    /// Reports AWS Rekognition configuration and optionally checks S3 and collection reachability.
    /// </summary>
    public async Task<AwsRekognitionStatusResponse> GetStatusAsync(
        bool checkRemote,
        CancellationToken cancellationToken)
    {
        var config = options.Value;
        var credentialSourcePresent = IsCredentialSourcePresent();
        var missingFields = GetMissingFields(config);
        var locallyConfigured = config.Enabled && config.FaceSearchEnabled && missingFields.Count == 0;

        if (!config.Enabled)
        {
            return CreateResponse(config, configured: false, credentialSourcePresent, [], InfrastructureResourceText.AwsRekognitionDisabled);
        }

        if (!config.FaceSearchEnabled)
        {
            return CreateResponse(config, configured: false, credentialSourcePresent, missingFields, InfrastructureResourceText.AwsRekognitionFaceSearchDisabled);
        }

        if (missingFields.Count > 0)
        {
            return CreateResponse(
                config,
                configured: false,
                credentialSourcePresent,
                missingFields,
                InfrastructureResourceText.MissingAwsRekognitionConfiguration(string.Join(", ", missingFields)));
        }

        if (!checkRemote)
        {
            return CreateResponse(config, locallyConfigured, credentialSourcePresent, missingFields, InfrastructureResourceText.AwsRekognitionLocalConfigComplete);
        }

        if (!credentialSourcePresent)
        {
            return CreateResponse(config, configured: false, credentialSourcePresent, missingFields, InfrastructureResourceText.AwsRekognitionCredentialsUnavailable);
        }

        try
        {
            var region = RegionEndpoint.GetBySystemName(config.Region);
            using var s3Client = new AmazonS3Client(region);
            using var rekognitionClient = new AmazonRekognitionClient(region);

            await s3Client.GetBucketLocationAsync(
                new GetBucketLocationRequest { BucketName = config.BucketName },
                cancellationToken);

            await rekognitionClient.DescribeCollectionAsync(
                new DescribeCollectionRequest { CollectionId = config.CollectionId },
                cancellationToken);

            return CreateResponse(config, configured: true, credentialSourcePresent: true, missingFields, InfrastructureResourceText.AwsRekognitionRemoteResourcesReachable);
        }
        catch (Exception exception)
        {
            return CreateResponse(config, configured: false, credentialSourcePresent, missingFields, ClassifyException(exception));
        }
    }

    private static IReadOnlyCollection<string> GetMissingFields(AwsRekognitionOptions options)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Region))
        {
            missing.Add(nameof(options.Region));
        }

        if (string.IsNullOrWhiteSpace(options.BucketName))
        {
            missing.Add(nameof(options.BucketName));
        }

        if (string.IsNullOrWhiteSpace(options.CollectionId))
        {
            missing.Add(nameof(options.CollectionId));
        }

        if (options.MinSimilarity is <= 0m or > 100m)
        {
            missing.Add(nameof(options.MinSimilarity));
        }

        return missing;
    }

    private static AwsRekognitionStatusResponse CreateResponse(
        AwsRekognitionOptions options,
        bool configured,
        bool credentialSourcePresent,
        IReadOnlyCollection<string> missingFields,
        string message)
    {
        return new AwsRekognitionStatusResponse(
            options.Enabled,
            configured,
            credentialSourcePresent,
            options.Region,
            options.BucketName,
            options.CollectionId,
            missingFields,
            message);
    }

    private static bool IsCredentialSourcePresent()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")) &&
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_PROFILE")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_RELATIVE_URI")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_CONTAINER_CREDENTIALS_FULL_URI")))
        {
            return true;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return !string.IsNullOrWhiteSpace(home) &&
            File.Exists(Path.Combine(home, ".aws", "credentials"));
    }

    private static string ClassifyException(Exception exception)
    {
        return exception switch
        {
            AmazonS3Exception { StatusCode: System.Net.HttpStatusCode.NotFound } => InfrastructureResourceText.AwsRekognitionBucketNotFound,
            AmazonS3Exception { StatusCode: System.Net.HttpStatusCode.Forbidden } => InfrastructureResourceText.AwsRekognitionPermissionDenied,
            ResourceNotFoundException => InfrastructureResourceText.AwsRekognitionCollectionNotFound,
            AmazonRekognitionException { StatusCode: System.Net.HttpStatusCode.Forbidden } => InfrastructureResourceText.AwsRekognitionPermissionDenied,
            AmazonServiceException { StatusCode: System.Net.HttpStatusCode.Forbidden } => InfrastructureResourceText.AwsRekognitionPermissionDenied,
            AmazonServiceException amazonServiceException => InfrastructureResourceText.AwsRekognitionRemoteCheckFailedWithStatus(amazonServiceException.StatusCode),
            AmazonClientException => InfrastructureResourceText.AwsRekognitionCredentialsUnavailable,
            _ => InfrastructureResourceText.AwsRekognitionRemoteCheckFailed
        };
    }
}
