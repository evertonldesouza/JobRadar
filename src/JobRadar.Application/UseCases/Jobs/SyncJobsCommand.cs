using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Jobs;

public record SyncJobsCommand : IRequest<int>;

public class SyncJobsCommandHandler : IRequestHandler<SyncJobsCommand, int>
{
    private readonly IEnumerable<IJobSource> _sources;
    private readonly IJobRepository _jobRepository;

    public SyncJobsCommandHandler(IEnumerable<IJobSource> sources, IJobRepository jobRepository)
    {
        _sources = sources;
        _jobRepository = jobRepository;
    }

    public async Task<int> Handle(SyncJobsCommand request, CancellationToken cancellationToken)
    {
        var total = 0;

        foreach (var source in _sources)
        {
            try
            {
                var jobs = await source.FetchJobsAsync();
                var newJobs = new List<Domain.Entities.Job>();

                foreach (var job in jobs)
                {
                    var urlExists = await _jobRepository.ExistsByUrlAsync(job.Url);
                    var titleExists = await _jobRepository.ExistsByTitleAndCompanyAsync(job.Title, job.Company);

                    if (!urlExists && !titleExists)
                        newJobs.Add(job);
                }

                if (newJobs.Count > 0)
                {
                    await _jobRepository.AddRangeAsync(newJobs);
                    total += newJobs.Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sync] Erro na fonte {source.Name}: {ex.Message}");
            }
        }

        return total;
    }
}