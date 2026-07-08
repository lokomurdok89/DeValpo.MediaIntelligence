namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class ImageBatchSearchJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ClientKey { get; set; } = string.Empty;
    public string ProfileKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ManifestJson { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int ProcessingAttempts { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public ICollection<ImageBatchSearchAsset> Assets { get; set; } = [];
    public ICollection<ImageBatchSearchMatch> Matches { get; set; } = [];
    public ICollection<ImageBatchSearchIssue> Issues { get; set; } = [];
}
