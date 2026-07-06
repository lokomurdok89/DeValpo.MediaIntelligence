namespace DeValpo.MediaIntelligence.Api.Validation;

public sealed record UploadedFileValidationResult(
    bool IsValid,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static UploadedFileValidationResult Valid { get; } = new(true);

    public static UploadedFileValidationResult Invalid(string errorCode, string errorMessage) =>
        new(false, errorCode, errorMessage);
}
