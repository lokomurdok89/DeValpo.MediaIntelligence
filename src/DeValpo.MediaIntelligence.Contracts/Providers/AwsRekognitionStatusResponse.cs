namespace DeValpo.MediaIntelligence.Contracts.Providers;

public sealed record AwsRekognitionStatusResponse(
    bool Enabled,
    bool Configured,
    bool CredentialSourcePresent,
    bool CredentialFilePresent,
    string Region,
    string BucketName,
    string CollectionId,
    string ProfileName,
    IReadOnlyCollection<string> MissingFields,
    string Message);
