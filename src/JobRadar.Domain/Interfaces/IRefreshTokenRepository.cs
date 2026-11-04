using JobRadar.Domain.Entities;

namespace JobRadar.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash);
    Task AddAsync(RefreshToken refreshToken);
    Task UpdateAsync(RefreshToken refreshToken);

    /// <summary>Revoga todas as sessões ativas do usuário (logout global / roubo de token / troca de senha).</summary>
    Task RevokeAllForUserAsync(Guid userId);
}
