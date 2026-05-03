using JobRadar.Domain.Entities;

namespace JobRadar.Domain.Interfaces;

public interface IJobRepository
{
    Task<(IEnumerable<Job> Jobs, int TotalCount)> GetAllAsync(
        string? technology = null,
        string? location = null,
        int page = 1,
        int pageSize = 20);
    Task<Job?> GetByIdAsync(Guid id);
    Task<bool> ExistsByUrlAsync(string url);
    Task AddRangeAsync(IEnumerable<Job> jobs);    
    Task<bool> ExistsByTitleAndCompanyAsync(string title, string company);
    Task<int> RemoveDuplicatesAsync();


}