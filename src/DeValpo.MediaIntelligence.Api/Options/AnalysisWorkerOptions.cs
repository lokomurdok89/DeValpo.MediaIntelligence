namespace DeValpo.MediaIntelligence.Api.Options;

public sealed class AnalysisWorkerOptions
{
    public bool Enabled { get; init; } = true;
    public int PollIntervalSeconds { get; init; } = 2;
}
