using JobRadar.Domain.Entities;

namespace JobRadar.Domain.Interfaces;

public interface IFavoriteRepository
{
    Task<IEnumerable<FavoriteJob>> GetByUserIdAsync(Guid userId);

    /// <summary>Ids das vagas já favoritadas pelo usuário (consulta enxuta, sem carregar a vaga).</summary>
    Task<IReadOnlyList<Guid>> GetJobIdsByUserIdAsync(Guid userId);
    Task<bool> ExistsAsync(Guid userId, Guid jobId);
    Task AddAsync(FavoriteJob favorite);
    Task AddRangeAsync(IEnumerable<FavoriteJob> favorites);
    Task RemoveAsync(Guid userId, Guid jobId);
}