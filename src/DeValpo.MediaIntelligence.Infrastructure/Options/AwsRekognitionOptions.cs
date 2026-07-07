namespace DeValpo.MediaIntelligence.Infrastructure.Options;

public sealed class AwsRekognitionOptions
{
    public bool Enabled { get; set; }
    public bool FaceSearchEnabled { get; set; }
    public string Region { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string CollectionId { get; set; } = string.Empty;
    public decimal MinSimilarity { get; set; } = 90m;
    public bool UsePolling { get; set; } = true;
    public string SnsTopicArn { get; set; } = string.Empty;
    public string RoleArn { get; set; } = string.Empty;
}
