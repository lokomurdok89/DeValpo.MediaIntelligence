namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class Client
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ClientKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
