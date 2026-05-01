using JobRadar.Domain.Entities;

namespace JobRadar.Domain.Interfaces;

public interface IJobRepository
{
    Task<IEnumerable<Job>> GetAllAsync(string? technology = null, string? location = null);
    Task<Job?> GetByIdAsync(Guid id);
    Task<bool> ExistsByUrlAsync(string url);
    Task AddRangeAsync(IEnumerable<Job> jobs);
}