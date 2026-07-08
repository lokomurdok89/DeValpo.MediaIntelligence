using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Providers;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using Google.Cloud.Vision.V1;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Google.Vision;

public sealed class GoogleVisionStatusService(
    IOptions<GoogleVisionOptions> options) : IGoogleVisionStatusService
{
    /// <summary>
    /// Reports Google Vision configuration and optional credential validation.
    /// </summary>
    public async Task<GoogleVisionStatusResponse> GetStatusAsync(
        bool checkRemote,
        CancellationToken cancellationToken)
    {
        var config = options.Value;
        var credentialFilePresent = IsCredentialFilePresent(config);
        var credentialSourcePresent = string.IsNullOrWhiteSpace(config.CredentialsPath) || credentialFilePresent;

        if (!config.Enabled)
        {
            return CreateResponse(config, configured: false, credentialSourcePresent, credentialFilePresent, InfrastructureResourceText.GoogleVisionDisabled);
        }

        if (!string.IsNullOrWhiteSpace(config.CredentialsPath) && !credentialFilePresent)
        {
            return CreateResponse(config, configured: false, credentialSourcePresent, credentialFilePresent, InfrastructureResourceText.GoogleVisionCredentialsFileNotFound);
        }

        if (!checkRemote)
        {
            return CreateResponse(config, configured: true, credentialSourcePresent, credentialFilePresent, InfrastructureResourceText.GoogleVisionLocalConfigComplete);
        }

        try
        {
            var credential = await GoogleVisionCredentialProvider.GetCredentialAsync(config, cancellationToken);
            await new ImageAnnotatorClientBuilder { GoogleCredential = credential }.BuildAsync(cancellationToken);

            return CreateResponse(config, configured: true, credentialSourcePresent, credentialFilePresent, InfrastructureResourceText.GoogleVisionCredentialsUsable);
        }
        catch (Exception exception)
        {
            return CreateResponse(config, configured: false, credentialSourcePresent, credentialFilePresent, ClassifyError(exception));
        }
    }

    private static bool IsCredentialFilePresent(GoogleVisionOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.CredentialsPath) && File.Exists(options.CredentialsPath);
    }

    private static GoogleVisionStatusResponse CreateResponse(
        GoogleVisionOptions options,
        bool configured,
        bool credentialSourcePresent,
        bool credentialFilePresent,
        string message)
    {
        return new GoogleVisionStatusResponse(
            options.Enabled,
            configured,
            credentialSourcePresent,
            credentialFilePresent,
            [],
            message);
    }

    private static string ClassifyError(Exception exception)
    {
        return exception switch
        {
            FileNotFoundException => InfrastructureResourceText.GoogleVisionCredentialsFileNotFound,
            UnauthorizedAccessException => InfrastructureResourceText.GoogleVisionCredentialsCannotBeRead,
            InvalidOperationException => InfrastructureResourceText.GoogleVisionCredentialsInvalid,
            RpcException { StatusCode: StatusCode.Unauthenticated } => InfrastructureResourceText.GoogleVisionCredentialsInvalidOrExpired,
            RpcException { StatusCode: StatusCode.PermissionDenied } => InfrastructureResourceText.GoogleVisionPermissionDenied,
            RpcException { StatusCode: StatusCode.FailedPrecondition } => InfrastructureResourceText.GoogleVisionApiOrBillingUnavailable,
            RpcException { StatusCode: StatusCode.ResourceExhausted } => InfrastructureResourceText.GoogleVisionQuotaReached,
            RpcException { StatusCode: StatusCode.Unavailable } => InfrastructureResourceText.GoogleVisionServiceUnavailable,
            RpcException rpcException => InfrastructureResourceText.GoogleVisionRequestFailedWithStatus(rpcException.StatusCode),
            _ => InfrastructureResourceText.GoogleVisionRequestFailed
        };
    }
}
