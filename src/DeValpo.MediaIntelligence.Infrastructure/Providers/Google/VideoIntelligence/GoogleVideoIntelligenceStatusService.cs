using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Providers;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using Google.Cloud.Storage.V1;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Google.VideoIntelligence;

public sealed class GoogleVideoIntelligenceStatusService(
    IOptions<GoogleVideoIntelligenceOptions> options) : IGoogleVideoIntelligenceStatusService
{
    /// <summary>
    /// Reports Google Video Intelligence configuration and optional staging bucket reachability.
    /// </summary>
    public async Task<GoogleVideoIntelligenceStatusResponse> GetStatusAsync(
        bool checkRemote,
        CancellationToken cancellationToken)
    {
        var config = options.Value;
        var missingFields = GetMissingFields(config);
        var credentialFilePresent = IsCredentialFilePresent(config);

        if (!config.Enabled)
        {
            return CreateResponse(config, configured: false, credentialFilePresent, missingFields, InfrastructureResourceText.GoogleVideoIntelligenceDisabled);
        }

        if (missingFields.Count > 0)
        {
            return CreateResponse(
                config,
                configured: false,
                credentialFilePresent,
                missingFields,
                InfrastructureResourceText.MissingGoogleVideoIntelligenceConfiguration(string.Join(", ", missingFields)));
        }

        if (!string.IsNullOrWhiteSpace(config.CredentialsPath) && !credentialFilePresent)
        {
            return CreateResponse(config, configured: false, credentialFilePresent, missingFields, InfrastructureResourceText.GoogleVideoIntelligenceCredentialsFileNotFound);
        }

        if (!checkRemote)
        {
            return CreateResponse(config, configured: true, credentialFilePresent, missingFields, InfrastructureResourceText.GoogleVideoIntelligenceLocalConfigComplete);
        }

        try
        {
            var credential = await GoogleVideoIntelligenceCredentialProvider.GetCredentialAsync(config, cancellationToken);
            var storageClient = await StorageClient.CreateAsync(credential);
            await storageClient.GetBucketAsync(config.BucketName, cancellationToken: cancellationToken);

            return CreateResponse(config, configured: true, credentialFilePresent, missingFields, InfrastructureResourceText.GoogleVideoIntelligenceBucketReachable);
        }
        catch (Exception exception)
        {
            return CreateResponse(config, configured: false, credentialFilePresent, missingFields, ClassifyError(exception));
        }
    }

    private static IReadOnlyCollection<string> GetMissingFields(GoogleVideoIntelligenceOptions options)
    {
        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(options.BucketName))
        {
            missingFields.Add(nameof(options.BucketName));
        }

        return missingFields;
    }

    private static bool IsCredentialFilePresent(GoogleVideoIntelligenceOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.CredentialsPath) && File.Exists(options.CredentialsPath);
    }

    private static GoogleVideoIntelligenceStatusResponse CreateResponse(
        GoogleVideoIntelligenceOptions options,
        bool configured,
        bool credentialFilePresent,
        IReadOnlyCollection<string> missingFields,
        string message)
    {
        return new GoogleVideoIntelligenceStatusResponse(
            options.Enabled,
            configured,
            credentialFilePresent,
            options.BucketName,
            missingFields,
            message);
    }

    private static string ClassifyError(Exception exception)
    {
        return exception switch
        {
            FileNotFoundException => InfrastructureResourceText.GoogleVideoIntelligenceCredentialsFileNotFound,
            UnauthorizedAccessException => InfrastructureResourceText.GoogleVideoIntelligenceCredentialsCannotBeRead,
            InvalidOperationException => InfrastructureResourceText.GoogleVideoIntelligenceCredentialsInvalid,
            RpcException { StatusCode: StatusCode.Unauthenticated } =>
                InfrastructureResourceText.GoogleVideoIntelligenceCredentialsInvalidOrExpired,
            RpcException { StatusCode: StatusCode.PermissionDenied } =>
                InfrastructureResourceText.GoogleVideoIntelligencePermissionDenied,
            RpcException { StatusCode: StatusCode.NotFound } =>
                InfrastructureResourceText.GoogleVideoIntelligenceBucketNotFound,
            RpcException { StatusCode: StatusCode.FailedPrecondition } =>
                InfrastructureResourceText.GoogleVideoIntelligenceApiOrBillingUnavailable,
            RpcException { StatusCode: StatusCode.ResourceExhausted } =>
                InfrastructureResourceText.GoogleVideoIntelligenceQuotaReached,
            RpcException { StatusCode: StatusCode.Unavailable } =>
                InfrastructureResourceText.GoogleVideoIntelligenceServiceUnavailable,
            RpcException rpcException =>
                InfrastructureResourceText.GoogleVideoIntelligenceRemoteCheckFailedWithStatus(rpcException.StatusCode),
            _ => InfrastructureResourceText.GoogleVideoIntelligenceRemoteCheckFailed
        };
    }
}
