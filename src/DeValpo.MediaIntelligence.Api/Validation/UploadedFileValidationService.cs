using DeValpo.MediaIntelligence.Api.Options;
using DeValpo.MediaIntelligence.Api.Resources;
using DeValpo.MediaIntelligence.Contracts;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Api.Validation;

public sealed class UploadedFileValidationService(
    IOptions<UploadValidationOptions> options,
    IUploadedFileThreatScanner threatScanner) : IUploadedFileValidationService
{
    private static readonly IReadOnlyDictionary<string, FileTypeRule> Rules =
        new Dictionary<string, FileTypeRule>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = new(
                [MediaAssetType.Document],
                ["application/pdf"],
                (head, length) => HasPdfHeader(head.AsSpan(0, length)),
                RequiresPdfEof: true),
            [".png"] = new(
                [MediaAssetType.Document, MediaAssetType.Image],
                ["image/png"],
                (head, length) => StartsWith(head.AsSpan(0, length), [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])),
            [".jpg"] = new(
                [MediaAssetType.Document, MediaAssetType.Image],
                ["image/jpeg"],
                (head, length) => StartsWith(head.AsSpan(0, length), [0xFF, 0xD8, 0xFF])),
            [".jpeg"] = new(
                [MediaAssetType.Document, MediaAssetType.Image],
                ["image/jpeg"],
                (head, length) => StartsWith(head.AsSpan(0, length), [0xFF, 0xD8, 0xFF])),
            [".webp"] = new(
                [MediaAssetType.Document, MediaAssetType.Image],
                ["image/webp"],
                (head, length) => HasWebpHeader(head.AsSpan(0, length))),
            [".tif"] = new(
                [MediaAssetType.Document, MediaAssetType.Image],
                ["image/tiff"],
                (head, length) => HasTiffHeader(head.AsSpan(0, length))),
            [".tiff"] = new(
                [MediaAssetType.Document, MediaAssetType.Image],
                ["image/tiff"],
                (head, length) => HasTiffHeader(head.AsSpan(0, length))),
            [".mp4"] = new(
                [MediaAssetType.Video, MediaAssetType.LiveVideo],
                ["video/mp4"],
                (head, length) => HasIsoBaseMediaHeader(head.AsSpan(0, length))),
            [".mov"] = new(
                [MediaAssetType.Video, MediaAssetType.LiveVideo],
                ["video/quicktime"],
                (head, length) => HasIsoBaseMediaHeader(head.AsSpan(0, length))),
            [".avi"] = new(
                [MediaAssetType.Video, MediaAssetType.LiveVideo],
                ["video/x-msvideo", "video/avi"],
                (head, length) => HasAviHeader(head.AsSpan(0, length)))
        };

    /// <summary>
    /// Validates upload size, declared type, binary signature and threat scan result.
    /// </summary>
    public async Task<UploadedFileValidationResult> ValidateAsync(
        IFormFile? file,
        MediaAssetType assetType,
        CancellationToken cancellationToken)
    {
        return await ValidateAsync(file, assetType, options.Value.MaxFileSizeBytes, cancellationToken);
    }

    /// <summary>
    /// Validates upload size, declared type, binary signature and threat scan result with a caller-specific size limit.
    /// </summary>
    public async Task<UploadedFileValidationResult> ValidateAsync(
        IFormFile? file,
        MediaAssetType assetType,
        long maxFileSizeBytes,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return UploadedFileValidationResult.Invalid(
                FileValidationErrorCodes.FileRequired,
                ApiResourceText.FileRequired);
        }

        if (file.Length == 0)
        {
            return UploadedFileValidationResult.Invalid(
                FileValidationErrorCodes.FileEmpty,
                ApiResourceText.FileEmpty);
        }

        var validationOptions = options.Value;
        if (file.Length > maxFileSizeBytes)
        {
            return UploadedFileValidationResult.Invalid(
                FileValidationErrorCodes.FileTooLarge,
                ApiResourceText.FileExceedsMaxSize(maxFileSizeBytes));
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !Rules.TryGetValue(extension, out var rule))
        {
            return UploadedFileValidationResult.Invalid(
                FileValidationErrorCodes.FileExtensionUnsupported,
                ApiResourceText.FileExtensionUnsupported(extension));
        }

        if (!rule.AssetTypes.Contains(assetType))
        {
            return UploadedFileValidationResult.Invalid(
                FileValidationErrorCodes.FileExtensionUnsupported,
                ApiResourceText.FileExtensionNotSupportedForAssetType(extension, assetType));
        }

        if (!HasSupportedDeclaredContentType(file.ContentType, rule.ContentTypes))
        {
            return UploadedFileValidationResult.Invalid(
                FileValidationErrorCodes.FileContentTypeUnsupported,
                ApiResourceText.FileContentTypeUnsupported(file.ContentType));
        }

        await using var stream = file.OpenReadStream();
        var head = new byte[16];
        var headLength = await stream.ReadAsync(head, cancellationToken);
        if (!rule.HeaderMatches(head, headLength))
        {
            return UploadedFileValidationResult.Invalid(
                FileValidationErrorCodes.FileSignatureInvalid,
                ApiResourceText.FileSignatureInvalid);
        }

        if (rule.RequiresPdfEof && !await HasPdfEofAsync(stream, validationOptions.PdfEofTailBytes, cancellationToken))
        {
            return UploadedFileValidationResult.Invalid(
                FileValidationErrorCodes.PdfCorrupt,
                ApiResourceText.PdfCorrupt);
        }

        var threatScanResult = await threatScanner.ScanAsync(file, cancellationToken);
        if (!threatScanResult.IsClean)
        {
            return UploadedFileValidationResult.Invalid(
                threatScanResult.ErrorCode ?? FileValidationErrorCodes.FileThreatDetected,
                threatScanResult.ErrorMessage ?? ApiResourceText.FileThreatDetected);
        }

        return UploadedFileValidationResult.Valid;
    }

    private static bool HasSupportedDeclaredContentType(string? contentType, IReadOnlyCollection<string> supportedContentTypes)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return true;
        }

        var normalizedContentType = contentType.Split(';', 2)[0].Trim();
        return string.Equals(normalizedContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase) ||
               supportedContentTypes.Contains(normalizedContentType, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<bool> HasPdfEofAsync(Stream stream, int tailBytes, CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
        {
            return false;
        }

        var readLength = (int)Math.Min(Math.Max(tailBytes, 1), stream.Length);
        stream.Seek(-readLength, SeekOrigin.End);

        var tail = new byte[readLength];
        var bytesRead = await stream.ReadAsync(tail, cancellationToken);

        return System.Text.Encoding.ASCII
            .GetString(tail, 0, bytesRead)
            .Contains("%%EOF", StringComparison.Ordinal);
    }

    private static bool HasPdfHeader(ReadOnlySpan<byte> head) =>
        StartsWith(head, System.Text.Encoding.ASCII.GetBytes("%PDF-"));

    private static bool HasWebpHeader(ReadOnlySpan<byte> head) =>
        head.Length >= 12 &&
        StartsWith(head, System.Text.Encoding.ASCII.GetBytes("RIFF")) &&
        head.Slice(8, 4).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("WEBP"));

    private static bool HasTiffHeader(ReadOnlySpan<byte> head) =>
        StartsWith(head, [0x49, 0x49, 0x2A, 0x00]) ||
        StartsWith(head, [0x4D, 0x4D, 0x00, 0x2A]);

    private static bool HasIsoBaseMediaHeader(ReadOnlySpan<byte> head) =>
        head.Length >= 12 &&
        head.Slice(4, 4).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("ftyp"));

    private static bool HasAviHeader(ReadOnlySpan<byte> head) =>
        head.Length >= 12 &&
        StartsWith(head, System.Text.Encoding.ASCII.GetBytes("RIFF")) &&
        head.Slice(8, 4).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("AVI "));

    private static bool StartsWith(ReadOnlySpan<byte> source, ReadOnlySpan<byte> expected) =>
        source.Length >= expected.Length && source[..expected.Length].SequenceEqual(expected);

    private sealed record FileTypeRule(
        MediaAssetType[] AssetTypes,
        string[] ContentTypes,
        Func<byte[], int, bool> HeaderMatches,
        bool RequiresPdfEof = false);
}
