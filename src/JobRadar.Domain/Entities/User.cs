namespace JobRadar.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public string? GoogleId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public List<FavoriteJob> Favorites { get; private set; } = [];

    protected User() { }

    public static User CreateWithPassword(string name, string email, string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static User CreateWithGoogle(string name, string email, string googleId)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            GoogleId = googleId,
            CreatedAt = DateTime.UtcNow
        };
    }
}