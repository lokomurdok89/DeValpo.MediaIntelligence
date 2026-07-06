namespace DeValpo.MediaIntelligence.Infrastructure.Options;

public sealed class GoogleDocumentAiOptions
{
    public bool Enabled { get; init; }
    public string ProjectId { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string ProcessorId { get; init; } = string.Empty;
    public string CredentialsPath { get; init; } = string.Empty;
}
