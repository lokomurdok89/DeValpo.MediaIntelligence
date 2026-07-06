using DeValpo.MediaIntelligence.Api.Options;
using DeValpo.MediaIntelligence.Api.Validation;
using DeValpo.MediaIntelligence.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DeValpo.MediaIntelligence.Tests;

public sealed class UploadedFileValidationServiceTests
{
    [Fact]
    public async Task ValidateAsync_WithValidPdf_ReturnsValid()
    {
        var service = CreateService();
        var file = CreateFormFile("%PDF-1.7\n1 0 obj\n%%EOF", "document.pdf", "application/pdf");

        var result = await service.ValidateAsync(file, MediaAssetType.Document, CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyFile_ReturnsMachineReadableError()
    {
        var service = CreateService();
        var file = CreateFormFile([], "empty.pdf", "application/pdf");

        var result = await service.ValidateAsync(file, MediaAssetType.Document, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("FILE_EMPTY");
    }

    [Fact]
    public async Task ValidateAsync_WithUnsupportedExtension_ReturnsMachineReadableError()
    {
        var service = CreateService();
        var file = CreateFormFile("not executable", "payload.exe", "application/octet-stream");

        var result = await service.ValidateAsync(file, MediaAssetType.Document, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("FILE_EXTENSION_UNSUPPORTED");
    }

    [Fact]
    public async Task ValidateAsync_WithContentTypeMismatch_ReturnsMachineReadableError()
    {
        var service = CreateService();
        var file = CreateFormFile("%PDF-1.7\n%%EOF", "document.pdf", "text/plain");

        var result = await service.ValidateAsync(file, MediaAssetType.Document, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("FILE_CONTENT_TYPE_UNSUPPORTED");
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidSignature_ReturnsMachineReadableError()
    {
        var service = CreateService();
        var file = CreateFormFile("plain text", "document.pdf", "application/pdf");

        var result = await service.ValidateAsync(file, MediaAssetType.Document, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("FILE_SIGNATURE_INVALID");
    }

    [Fact]
    public async Task ValidateAsync_WithCorruptPdf_ReturnsMachineReadableError()
    {
        var service = CreateService();
        var file = CreateFormFile("%PDF-1.7\n1 0 obj", "document.pdf", "application/pdf");

        var result = await service.ValidateAsync(file, MediaAssetType.Document, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("PDF_CORRUPT");
    }

    [Fact]
    public async Task ValidateAsync_WithOversizedFile_ReturnsMachineReadableError()
    {
        var service = CreateService(new UploadValidationOptions { MaxFileSizeBytes = 8 });
        var file = CreateFormFile("%PDF-1.7\n%%EOF", "document.pdf", "application/pdf");

        var result = await service.ValidateAsync(file, MediaAssetType.Document, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("FILE_TOO_LARGE");
    }

    [Fact]
    public async Task ValidateAsync_WhenThreatScannerFails_ReturnsMachineReadableError()
    {
        var service = CreateService(scanner: new BlockingThreatScanner());
        var file = CreateFormFile("%PDF-1.7\n%%EOF", "document.pdf", "application/pdf");

        var result = await service.ValidateAsync(file, MediaAssetType.Document, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("FILE_THREAT_DETECTED");
    }

    private static UploadedFileValidationService CreateService(
        UploadValidationOptions? options = null,
        IUploadedFileThreatScanner? scanner = null)
    {
        return new UploadedFileValidationService(
            Options.Create(options ?? new UploadValidationOptions()),
            scanner ?? new NoOpUploadedFileThreatScanner());
    }

    private static IFormFile CreateFormFile(string content, string fileName, string contentType)
    {
        return CreateFormFile(System.Text.Encoding.ASCII.GetBytes(content), fileName, contentType);
    }

    private static IFormFile CreateFormFile(byte[] content, string fileName, string contentType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class BlockingThreatScanner : IUploadedFileThreatScanner
    {
        public Task<FileThreatScanResult> ScanAsync(IFormFile file, CancellationToken cancellationToken)
        {
            return Task.FromResult(new FileThreatScanResult(false, "FILE_THREAT_DETECTED", null));
        }
    }
}
