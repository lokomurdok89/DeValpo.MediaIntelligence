namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class VideoSearchJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ClientKey { get; set; } = string.Empty;
    public string ProfileKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string ManifestJson { get; set; } = string.Empty;
    public string? ConsentReferenceId { get; set; }
    public string? ErrorMessage { get; set; }
    public int ProcessingAttempts { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public List<VideoSearchAsset> Assets { get; set; } = [];
    public List<VideoSearchTargetMatchSegment> TargetMatchSegments { get; set; } = [];
    public List<VideoSearchFaceOccurrenceSegment> FaceOccurrenceSegments { get; set; } = [];
    public List<VideoSearchFaceReferenceMatchSegment> FaceReferenceMatchSegments { get; set; } = [];
    public List<VideoSearchIssue> Issues { get; set; } = [];
}
