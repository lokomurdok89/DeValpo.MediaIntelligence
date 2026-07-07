using DeValpo.MediaIntelligence.Contracts.Providers;

namespace DeValpo.MediaIntelligence.Application.Abstractions;

public interface IAwsRekognitionStatusService
{
    /// <summary>
    /// Returns local AWS Rekognition configuration status and optionally validates remote AWS resources.
    /// </summary>
    Task<AwsRekognitionStatusResponse> GetStatusAsync(
        bool checkRemote,
        CancellationToken cancellationToken);
}
