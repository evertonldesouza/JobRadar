using JobRadar.Domain.Entities;

namespace JobRadar.Domain.Interfaces;

public interface IJobSource
{
    string Name { get; }
    Task<IEnumerable<Job>> FetchJobsAsync();
}
