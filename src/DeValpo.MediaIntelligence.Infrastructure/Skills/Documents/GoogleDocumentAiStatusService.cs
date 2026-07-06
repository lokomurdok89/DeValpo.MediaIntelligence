using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Providers;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using Google.Cloud.DocumentAI.V1;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills.Documents;

public sealed class GoogleDocumentAiStatusService(
    IOptions<GoogleDocumentAiOptions> options,
    IGoogleDocumentAiClientFactory clientFactory) : IGoogleDocumentAiStatusService
{
    /// <summary>
    /// Evaluates local provider settings and optionally verifies the configured processor remotely.
    /// </summary>
    public async Task<GoogleDocumentAiStatusResponse> GetStatusAsync(
        bool checkRemote,
        CancellationToken cancellationToken)
    {
        var config = options.Value;
        var missingFields = GetMissingFields(config);
        var credentialFilePresent = IsCredentialFilePresent(config);

        if (!config.Enabled)
        {
            return CreateResponse(
                config,
                configured: false,
                credentialFilePresent,
                missingFields,
                InfrastructureResourceText.GoogleDocumentAiDisabled);
        }

        if (missingFields.Count > 0)
        {
            return CreateResponse(
                config,
                configured: false,
                credentialFilePresent,
                missingFields,
                InfrastructureResourceText.MissingGoogleDocumentAiConfiguration(string.Join(", ", missingFields)));
        }

        if (!string.IsNullOrWhiteSpace(config.CredentialsPath) && !credentialFilePresent)
        {
            return CreateResponse(
                config,
                configured: false,
                credentialFilePresent,
                missingFields,
                InfrastructureResourceText.GoogleDocumentAiCredentialsFileNotFound);
        }

        if (!checkRemote)
        {
            return CreateResponse(
                config,
                configured: true,
                credentialFilePresent,
                missingFields,
                string.IsNullOrWhiteSpace(config.CredentialsPath)
                    ? InfrastructureResourceText.GoogleDocumentAiLocalConfigCompleteWithAdc
                    : InfrastructureResourceText.GoogleDocumentAiLocalConfigComplete);
        }

        try
        {
            var client = await clientFactory.CreateAsync(config, cancellationToken);
            await client.GetProcessorAsync(
                new GetProcessorRequest
                {
                    Name = clientFactory.CreateProcessorName(config).ToString()
                },
                cancellationToken);

            return CreateResponse(
                config,
                configured: true,
                credentialFilePresent,
                missingFields,
                InfrastructureResourceText.GoogleDocumentAiProcessorReachable);
        }
        catch (Exception exception)
        {
            return CreateResponse(
                config,
                configured: false,
                credentialFilePresent,
                missingFields,
                ClassifyRemoteError(exception));
        }
    }

    /// <summary>
    /// Returns the provider fields required before Document AI can run.
    /// </summary>
    private static IReadOnlyCollection<string> GetMissingFields(GoogleDocumentAiOptions options)
    {
        var missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ProjectId))
        {
            missingFields.Add(nameof(options.ProjectId));
        }

        if (string.IsNullOrWhiteSpace(options.Location))
        {
            missingFields.Add(nameof(options.Location));
        }

        if (string.IsNullOrWhiteSpace(options.ProcessorId))
        {
            missingFields.Add(nameof(options.ProcessorId));
        }

        return missingFields;
    }

    /// <summary>
    /// Checks whether the optional configured credentials file exists.
    /// </summary>
    private static bool IsCredentialFilePresent(GoogleDocumentAiOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.CredentialsPath) && File.Exists(options.CredentialsPath);
    }

    /// <summary>
    /// Creates the status response without exposing credential content.
    /// </summary>
    private static GoogleDocumentAiStatusResponse CreateResponse(
        GoogleDocumentAiOptions options,
        bool configured,
        bool credentialFilePresent,
        IReadOnlyCollection<string> missingFields,
        string message)
    {
        return new GoogleDocumentAiStatusResponse(
            options.Enabled,
            configured,
            credentialFilePresent,
            options.ProjectId,
            options.Location,
            options.ProcessorId,
            missingFields,
            message);
    }

    /// <summary>
    /// Converts SDK, credential and filesystem failures into API-safe status messages.
    /// </summary>
    private static string ClassifyRemoteError(Exception exception)
    {
        return exception switch
        {
            FileNotFoundException => InfrastructureResourceText.GoogleDocumentAiCredentialsFileNotFound,
            UnauthorizedAccessException => InfrastructureResourceText.GoogleDocumentAiCredentialsCannotBeRead,
            InvalidOperationException => InfrastructureResourceText.GoogleDocumentAiCredentialsInvalid,
            RpcException { StatusCode: StatusCode.Unauthenticated } =>
                InfrastructureResourceText.GoogleDocumentAiCredentialsInvalidOrExpired,
            RpcException { StatusCode: StatusCode.PermissionDenied } =>
                InfrastructureResourceText.GoogleDocumentAiPermissionDenied,
            RpcException { StatusCode: StatusCode.NotFound } =>
                InfrastructureResourceText.GoogleDocumentAiProcessorNotFound,
            RpcException { StatusCode: StatusCode.FailedPrecondition } =>
                InfrastructureResourceText.GoogleDocumentAiApiOrBillingUnavailable,
            RpcException { StatusCode: StatusCode.ResourceExhausted } =>
                InfrastructureResourceText.GoogleDocumentAiQuotaReached,
            RpcException { StatusCode: StatusCode.Unavailable } =>
                InfrastructureResourceText.GoogleDocumentAiServiceUnavailable,
            RpcException rpcException =>
                InfrastructureResourceText.GoogleDocumentAiRemoteCheckFailedWithStatus(rpcException.StatusCode),
            _ => InfrastructureResourceText.GoogleDocumentAiRemoteCheckFailed
        };
    }
}
