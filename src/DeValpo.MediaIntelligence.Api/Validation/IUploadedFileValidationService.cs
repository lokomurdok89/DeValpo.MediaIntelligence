using DeValpo.MediaIntelligence.Contracts;

namespace DeValpo.MediaIntelligence.Api.Validation;

public interface IUploadedFileValidationService
{
    /// <summary>
    /// Validates the uploaded file before the API stores or queues it for analysis.
    /// </summary>
    Task<UploadedFileValidationResult> ValidateAsync(
        IFormFile? file,
        MediaAssetType assetType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates the uploaded file with a caller-specific maximum size.
    /// </summary>
    Task<UploadedFileValidationResult> ValidateAsync(
        IFormFile? file,
        MediaAssetType assetType,
        long maxFileSizeBytes,
        CancellationToken cancellationToken);
}
