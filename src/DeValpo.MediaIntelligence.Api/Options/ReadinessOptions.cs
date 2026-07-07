namespace DeValpo.MediaIntelligence.Api.Options;

public sealed class ReadinessOptions
{
    public bool RequireGoogleDocumentAi { get; set; }
    public bool CheckGoogleDocumentAiRemote { get; set; }
    public int TimeoutSeconds { get; set; } = 10;
}
