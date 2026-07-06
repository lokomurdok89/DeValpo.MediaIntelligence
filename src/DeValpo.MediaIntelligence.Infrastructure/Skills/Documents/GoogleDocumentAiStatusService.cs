using DeValpo.MediaIntelligence.Application.Abstractions;
using DeValpo.MediaIntelligence.Contracts.Providers;
using DeValpo.MediaIntelligence.Infrastructure.Options;
using Google.Cloud.DocumentAI.V1;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Infrastructure.Skills.Documents;

public sealed class GoogleDocumentAiStatusService(
    IOptions<GoogleDocumentAiOptions> options,
    IGoogleDocumentAiClientFactory clientFactory) : IGoogleDocumentAiStatusService
{
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
                "Google Document AI is disabled.");
        }

        if (missingFields.Count > 0)
        {
            return CreateResponse(
                config,
                configured: false,
                credentialFilePresent,
                missingFields,
                $"Missing Google Document AI configuration: {string.Join(", ", missingFields)}.");
        }

        if (!string.IsNullOrWhiteSpace(config.CredentialsPath) && !credentialFilePresent)
        {
            return CreateResponse(
                config,
                configured: false,
                credentialFilePresent,
                missingFields,
                "Google Document AI credentials file was not found.");
        }

        if (!checkRemote)
        {
            return CreateResponse(
                config,
                configured: true,
                credentialFilePresent,
                missingFields,
                string.IsNullOrWhiteSpace(config.CredentialsPath)
                    ? "Google Document AI local configuration is complete. Authentication will use Application Default Credentials."
                    : "Google Document AI local configuration is complete.");
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
                "Google Document AI processor is reachable.");
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

    private static bool IsCredentialFilePresent(GoogleDocumentAiOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.CredentialsPath) && File.Exists(options.CredentialsPath);
    }

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

    private static string ClassifyRemoteError(Exception exception)
    {
        return exception switch
        {
            FileNotFoundException => "Google Document AI credentials file was not found.",
            UnauthorizedAccessException => "Google Document AI credentials file cannot be read.",
            InvalidOperationException => "Google Document AI credentials are invalid.",
            RpcException { StatusCode: StatusCode.Unauthenticated } =>
                "Google Document AI credentials are invalid or expired.",
            RpcException { StatusCode: StatusCode.PermissionDenied } =>
                "Google Document AI service account does not have permission for this processor.",
            RpcException { StatusCode: StatusCode.NotFound } =>
                "Google Document AI processor was not found.",
            RpcException { StatusCode: StatusCode.FailedPrecondition } =>
                "Google Document AI API or billing is not available for this project.",
            RpcException { StatusCode: StatusCode.ResourceExhausted } =>
                "Google Document AI quota or billing limit was reached.",
            RpcException { StatusCode: StatusCode.Unavailable } =>
                "Google Document AI service is unavailable.",
            RpcException rpcException =>
                $"Google Document AI remote check failed: {rpcException.StatusCode}.",
            _ => "Google Document AI remote check failed."
        };
    }
}
