namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class AnalysisProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProfileKey { get; set; } = string.Empty;
    public string ClientKey { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefinitionJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
