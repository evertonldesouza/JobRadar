using JobRadar.Application.Abstractions;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Jobs;

public record SyncJobsCommand : IRequest<int>;

public class SyncJobsCommandHandler : IRequestHandler<SyncJobsCommand, int>
{
    private readonly IEnumerable<IJobSource> _sources;
    private readonly IJobRepository _jobRepository;
    private readonly IEmailService _emailService;
    private readonly IJobAlertRepository _alertRepository;

    public SyncJobsCommandHandler(
        IEnumerable<IJobSource> sources,
        IJobRepository jobRepository,
        IEmailService emailService,
        IJobAlertRepository alertRepository)
    {
        _sources = sources;
        _jobRepository = jobRepository;
        _emailService = emailService;
        _alertRepository = alertRepository;
    }

    public async Task<int> Handle(SyncJobsCommand request, CancellationToken cancellationToken)
    {
        var total = 0;
        var allNewJobs = new List<Job>();

        foreach (var source in _sources)
        {
            try
            {
                var jobs = await source.FetchJobsAsync();
                var newJobs = new List<Job>();

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
                    allNewJobs.AddRange(newJobs);
                    JobsCache.Version++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sync] Erro na fonte {source.Name}: {ex.Message}");
            }
        }

        if (allNewJobs.Count > 0)
            await NotifyAlertsAsync(allNewJobs);

        return total;
    }

    private async Task NotifyAlertsAsync(List<Job> newJobs)
    {
        var alerts = await _alertRepository.GetAllAsync();

        foreach (var alert in alerts)
        {
            var matchingJobs = newJobs.Where(j =>
                (alert.Technology == null || j.Technologies.Any(t =>
                    t.Contains(alert.Technology, StringComparison.OrdinalIgnoreCase))) &&
                (alert.Location == null || j.Location.Contains(alert.Location, StringComparison.OrdinalIgnoreCase)) &&
                (alert.Source == null || j.Source.Equals(alert.Source, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            foreach (var job in matchingJobs)
            {
                await _emailService.SendJobAlertAsync(alert.User, job, alert);
            }
        }
    }
}
