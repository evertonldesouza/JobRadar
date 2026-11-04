using JobRadar.Domain.Entities;

namespace JobRadar.Application.Abstractions;

public interface ITokenService
{
    string GenerateToken(User user);

    /// <summary>Gera um token opaco criptograficamente aleatório (refresh / recuperação).</summary>
    string GenerateRefreshToken();
}
