using JobRadar.Domain.Entities;

namespace JobRadar.Domain.Interfaces;

public interface IFavoriteRepository
{
    Task<IEnumerable<FavoriteJob>> GetByUserIdAsync(Guid userId);
    Task<bool> ExistsAsync(Guid userId, Guid jobId);
    Task AddAsync(FavoriteJob favorite);
    Task RemoveAsync(Guid userId, Guid jobId);
}