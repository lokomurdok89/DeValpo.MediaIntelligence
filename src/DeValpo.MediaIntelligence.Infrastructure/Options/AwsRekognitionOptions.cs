namespace DeValpo.MediaIntelligence.Infrastructure.Options;

public sealed class AwsRekognitionOptions
{
    public bool Enabled { get; set; }
    public bool FaceSearchEnabled { get; set; }
    public string Region { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string CollectionId { get; set; } = string.Empty;
    public string CredentialsPath { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;
    public bool AllowUserCredentials { get; set; }
    public decimal MinSimilarity { get; set; } = 90m;
    public bool UsePolling { get; set; } = true;
    public int PollingIntervalSeconds { get; set; } = 5;
    public int PollingTimeoutSeconds { get; set; } = 900;
    public string StagingPrefix { get; set; } = "staging";
    public bool DeleteStagedObjectsAfterProcessing { get; set; }
    public string SnsTopicArn { get; set; } = string.Empty;
    public string RoleArn { get; set; } = string.Empty;
}
