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
            var jobs = await source.FetchJobsAsync();
            var newJobs = new List<Domain.Entities.Job>();

            foreach (var job in jobs)
            {
                if (!await _jobRepository.ExistsByUrlAsync(job.Url))
                    newJobs.Add(job);
            }

            if (newJobs.Count > 0)
            {
                await _jobRepository.AddRangeAsync(newJobs);
                total += newJobs.Count;
            }
        }

        return total;
    }
}