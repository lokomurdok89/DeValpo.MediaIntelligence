namespace DeValpo.MediaIntelligence.Api.Validation;

public sealed record FileThreatScanResult(
    bool IsClean,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static FileThreatScanResult Clean { get; } = new(true);
}
