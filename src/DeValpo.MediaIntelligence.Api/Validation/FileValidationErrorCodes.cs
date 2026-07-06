namespace DeValpo.MediaIntelligence.Api.Validation;

internal static class FileValidationErrorCodes
{
    public const string FileRequired = "FILE_REQUIRED";
    public const string FileEmpty = "FILE_EMPTY";
    public const string FileTooLarge = "FILE_TOO_LARGE";
    public const string FileExtensionUnsupported = "FILE_EXTENSION_UNSUPPORTED";
    public const string FileContentTypeUnsupported = "FILE_CONTENT_TYPE_UNSUPPORTED";
    public const string FileSignatureInvalid = "FILE_SIGNATURE_INVALID";
    public const string PdfCorrupt = "PDF_CORRUPT";
    public const string FileThreatDetected = "FILE_THREAT_DETECTED";
}
