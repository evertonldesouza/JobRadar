using JobRadar.Domain.Entities;

namespace JobRadar.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByGoogleIdAsync(string googleId);
    Task<User?> GetByIdAsync(Guid id);
    Task AddAsync(User user);
}