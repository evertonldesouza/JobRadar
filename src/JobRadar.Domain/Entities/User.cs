namespace JobRadar.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public string? GoogleId { get; private set; }
    public string? PasswordResetTokenHash { get; private set; }
    public DateTime? PasswordResetTokenExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public List<FavoriteJob> Favorites { get; private set; } = [];
    public List<JobAlert> Alerts { get; private set; } = [];
    public List<RefreshToken> RefreshTokens { get; private set; } = [];

    protected User() { }

    public static User CreateWithPassword(string name, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Hash da senha é obrigatório.", nameof(passwordHash));

        return new User
        {
            Id = Guid.NewGuid(),
            Name = NormalizeName(name),
            Email = NormalizeEmail(email),
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static User CreateWithGoogle(string name, string email, string googleId)
    {
        if (string.IsNullOrWhiteSpace(googleId))
            throw new ArgumentException("GoogleId é obrigatório.", nameof(googleId));

        return new User
        {
            Id = Guid.NewGuid(),
            Name = NormalizeName(name),
            Email = NormalizeEmail(email),
            GoogleId = googleId.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Define uma nova senha (hash BCrypt) — cadastro, reset ou troca.</summary>
    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Hash da senha é obrigatório.", nameof(passwordHash));

        PasswordHash = passwordHash;
        ClearPasswordResetToken();
    }

    /// <summary>Registra um pedido de recuperação (apenas o hash do token é persistido).</summary>
    public void RequestPasswordReset(string tokenHash, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Hash do token é obrigatório.", nameof(tokenHash));

        PasswordResetTokenHash = tokenHash;
        PasswordResetTokenExpiresAt = DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc);
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresAt = null;
    }

    public bool HasValidPasswordResetToken =>
        PasswordResetTokenHash is not null &&
        PasswordResetTokenExpiresAt is not null &&
        PasswordResetTokenExpiresAt > DateTime.UtcNow;

    /// <summary>Vincula uma conta Google a um usuário que já existia (ex.: cadastro por email).</summary>
    public void LinkGoogleAccount(string googleId)
    {
        if (string.IsNullOrWhiteSpace(googleId))
            throw new ArgumentException("GoogleId é obrigatório.", nameof(googleId));

        GoogleId = googleId.Trim();
    }

    public static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email é obrigatório.", nameof(email));

        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome é obrigatório.", nameof(name));

        return name.Trim();
    }
}
