namespace DeValpo.MediaIntelligence.Contracts.Providers;

public sealed record AwsRekognitionStatusResponse(
    bool Enabled,
    bool Configured,
    bool CredentialSourcePresent,
    string Region,
    string BucketName,
    string CollectionId,
    IReadOnlyCollection<string> MissingFields,
    string Message);
