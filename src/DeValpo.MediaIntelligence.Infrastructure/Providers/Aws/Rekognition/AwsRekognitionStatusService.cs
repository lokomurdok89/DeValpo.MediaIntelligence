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
    IOptions<AwsRekognitionOptions> options,
    IAwsCredentialProvider credentialProvider,
    IAwsRekognitionClientFactory clientFactory) : IAwsRekognitionStatusService
{
    /// <summary>
    /// Reports AWS Rekognition configuration and optionally checks S3 and collection reachability.
    /// </summary>
    public async Task<AwsRekognitionStatusResponse> GetStatusAsync(
        bool checkRemote,
        CancellationToken cancellationToken)
    {
        var config = options.Value;
        var credentialSourcePresent = credentialProvider.IsCredentialSourcePresent(config);
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
            using var s3Client = clientFactory.CreateS3Client(config);
            using var rekognitionClient = clientFactory.CreateRekognitionClient(config);

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

        if (!options.UsePolling)
        {
            missing.Add(nameof(options.UsePolling));
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
            IsCredentialFilePresent(options),
            options.Region,
            options.BucketName,
            options.CollectionId,
            options.ProfileName,
            missingFields,
            message);
    }

    private static bool IsCredentialFilePresent(AwsRekognitionOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.CredentialsPath) && File.Exists(options.CredentialsPath);
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
