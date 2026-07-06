namespace DeValpo.MediaIntelligence.Api.Validation;

public sealed class NoOpUploadedFileThreatScanner : IUploadedFileThreatScanner
{
    /// <summary>
    /// Returns a clean scan result until a real malware scanning provider is wired in.
    /// </summary>
    public Task<FileThreatScanResult> ScanAsync(IFormFile file, CancellationToken cancellationToken)
    {
        return Task.FromResult(FileThreatScanResult.Clean);
    }
}
