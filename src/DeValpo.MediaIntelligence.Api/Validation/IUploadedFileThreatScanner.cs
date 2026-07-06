namespace DeValpo.MediaIntelligence.Api.Validation;

public interface IUploadedFileThreatScanner
{
    /// <summary>
    /// Scans an uploaded file for threats before it is persisted.
    /// </summary>
    Task<FileThreatScanResult> ScanAsync(IFormFile file, CancellationToken cancellationToken);
}
