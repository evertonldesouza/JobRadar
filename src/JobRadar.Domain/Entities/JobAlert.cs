namespace JobRadar.Domain.Entities;

public class JobAlert
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string? Technology { get; private set; }
    public string? Location { get; private set; }
    public string? Source { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public User User { get; private set; } = null!;

    protected JobAlert() { }

    public static JobAlert Create(Guid userId, string? technology, string? location, string? source)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Usuário é obrigatório.", nameof(userId));

        return new JobAlert
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Technology = Normalize(technology),
            Location = Normalize(location),
            Source = Normalize(source),
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
