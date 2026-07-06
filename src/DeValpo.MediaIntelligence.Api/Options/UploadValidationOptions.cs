namespace DeValpo.MediaIntelligence.Api.Options;

public sealed class UploadValidationOptions
{
    public long MaxFileSizeBytes { get; init; } = 104_857_600;
    public int PdfEofTailBytes { get; init; } = 4096;
}
