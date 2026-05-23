using JobRadar.Domain.Entities;

namespace JobRadar.Domain.Interfaces;

public interface IJobAlertRepository
{
    Task<List<JobAlert>> GetByUserIdAsync(Guid userId);
    Task<List<JobAlert>> GetAllAsync();
    Task<JobAlert?> GetByIdAsync(Guid id);
    Task AddAsync(JobAlert alert);
    Task RemoveAsync(JobAlert alert);
}
