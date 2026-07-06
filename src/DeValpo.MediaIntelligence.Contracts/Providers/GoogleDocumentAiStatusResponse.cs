namespace DeValpo.MediaIntelligence.Contracts.Providers;

public sealed record GoogleDocumentAiStatusResponse(
    bool Enabled,
    bool Configured,
    bool CredentialFilePresent,
    string ProjectId,
    string Location,
    string ProcessorId,
    IReadOnlyCollection<string> MissingFields,
    string Message);
