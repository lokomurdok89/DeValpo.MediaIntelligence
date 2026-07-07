namespace DeValpo.MediaIntelligence.Domain.Entities;

public sealed class ClientRetentionPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClientId { get; set; }
    public Client? Client { get; set; }
    public bool Enabled { get; set; }
    public int RetentionDays { get; set; } = 365;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
