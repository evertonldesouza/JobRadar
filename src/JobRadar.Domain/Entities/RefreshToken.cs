namespace JobRadar.Domain.Entities;

/// <summary>
/// Refresh token persistido apenas como hash. A cada uso ele é rotacionado:
/// o antigo é revogado e aponta para o substituto. Reuso de token revogado
/// é tratado como possível roubo (revoga todas as sessões do usuário).
/// </summary>
public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public User User { get; private set; } = null!;

    protected RefreshToken() { }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Usuário é obrigatório.", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Hash do token é obrigatório.", nameof(tokenHash));

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
    public bool WasRevoked => RevokedAt is not null;

    public void Revoke(string? replacedByTokenHash = null)
    {
        if (RevokedAt is not null)
            return;

        RevokedAt = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
