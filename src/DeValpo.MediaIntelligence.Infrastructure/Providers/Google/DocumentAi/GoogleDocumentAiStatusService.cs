using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Providers;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using DeValpo.MediaIntelligence.Infrastructure.Resources;
using Google.Cloud.DocumentAI.V1;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Providers.Google.DocumentAi;

public sealed class GoogleDocumentAiStatusService(
    IOptions<GoogleDocumentAiOptions> options,
    IGoogleDocumentAiClientFactory clientFactory,
    IMemoryCache? memoryCache = null,
    IOptions<GoogleDocumentAiStatusCacheOptions>? cacheOptions = null) : IGoogleDocumentAiStatusService
{
    private const string RemoteStatusCacheKeyPrefix = "google-document-ai-status";

    /// <summary>
    /// Evaluates local provider settings and optionally verifies the configured processor remotely.
    /// </summary>
    public async Task<GoogleDocumentAiStatusResponse> GetStatusAsync(
        bool checkRemote,
        CancellationToken cancellationToken)
    {
        if (!checkRemote || memoryCache is null)
        {
            return await EvaluateStatusAsync(checkRemote, cancellationToken);
        }

        var config = options.Value;
        var cacheKey = string.Join(
            "|",
            RemoteStatusCacheKeyPrefix,
            config.Enabled,
            config.ProjectId,
            config.Location,
            config.ProcessorId,
            config.CredentialsPath);

        if (memoryCache.TryGetValue(cacheKey, out GoogleDocumentAiStatusResponse? cached) && cached is not null)
        {
            return cached;
        }

        var status = await EvaluateStatusAsync(checkRemote: true, cancellationToken);
        memoryCache.Set(
            cacheKey,
            status,
            TimeSpan.FromSeconds(Math.Max(cacheOptions?.Value.RemoteStatusTtlSeconds ?? 60, 1)));

        return status;
    }

    private async Task<GoogleDocumentAiStatusResponse> EvaluateStatusAsync(
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
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(cacheOptions?.Value.RemoteStatusTimeoutSeconds ?? 10, 1)));

            var client = await clientFactory.CreateAsync(config, timeoutCts.Token);
            await client.GetProcessorAsync(
                new GetProcessorRequest
                {
                    Name = clientFactory.CreateProcessorName(config).ToString()
                },
                timeoutCts.Token);

            return CreateResponse(
                config,
                configured: true,
                credentialFilePresent,
                missingFields,
                InfrastructureResourceText.GoogleDocumentAiProcessorReachable);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CreateResponse(
                config,
                configured: false,
                credentialFilePresent,
                missingFields,
                InfrastructureResourceText.GoogleDocumentAiRemoteCheckTimedOut);
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
