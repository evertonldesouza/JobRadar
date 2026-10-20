using JobRadar.Application.Abstractions;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace JobRadar.Application.UseCases.Jobs;

public record SyncJobsCommand : IRequest<int>;

public sealed class SyncJobsCommandHandler : IRequestHandler<SyncJobsCommand, int>
{
    private readonly IEnumerable<IJobSource> _sources;
    private readonly IJobRepository _jobRepository;
    private readonly IEmailService _emailService;
    private readonly IJobAlertRepository _alertRepository;
    private readonly ILogger<SyncJobsCommandHandler> _logger;

    public SyncJobsCommandHandler(
        IEnumerable<IJobSource> sources,
        IJobRepository jobRepository,
        IEmailService emailService,
        IJobAlertRepository alertRepository,
        ILogger<SyncJobsCommandHandler> logger)
    {
        _sources = sources;
        _jobRepository = jobRepository;
        _emailService = emailService;
        _alertRepository = alertRepository;
        _logger = logger;
    }

    public async Task<int> Handle(SyncJobsCommand request, CancellationToken cancellationToken)
    {
        // Busca todas as fontes em paralelo; a falha de uma fonte não derruba as demais.
        var fetched = await Task.WhenAll(_sources.Select(FetchSafeAsync));
        var candidates = fetched.SelectMany(jobs => jobs).ToList();

        if (candidates.Count == 0)
        {
            _logger.LogInformation("Sync finalizado: nenhuma vaga retornada pelas fontes");
            return 0;
        }

        // Deduplica dentro do próprio lote (a mesma vaga pode vir de mais de uma fonte).
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        candidates = candidates
            .Where(job => seenUrls.Add(job.Url) && seenKeys.Add(job.DeduplicationKey))
            .ToList();

        // Uma única consulta por critério (antes: 2 consultas por vaga — N+1).
        var existingUrls = new HashSet<string>(
            await _jobRepository.GetExistingUrlsAsync(candidates.Select(j => j.Url).ToList(), cancellationToken),
            StringComparer.OrdinalIgnoreCase);
        var existingKeys = new HashSet<string>(
            await _jobRepository.GetExistingDeduplicationKeysAsync(candidates.Select(j => j.Title).Distinct().ToList(), cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        var newJobs = candidates
            .Where(job => !existingUrls.Contains(job.Url) && !existingKeys.Contains(job.DeduplicationKey))
            .ToList();

        if (newJobs.Count > 0)
        {
            await _jobRepository.AddRangeAsync(newJobs);
            JobsCache.Invalidate();
            await NotifyAlertsAsync(newJobs);
        }

        _logger.LogInformation(
            "Sync finalizado: {New} novas vagas ({Candidates} avaliadas, {Sources} fontes)",
            newJobs.Count, candidates.Count, _sources.Count());

        return newJobs.Count;
    }

    private async Task<IEnumerable<Job>> FetchSafeAsync(IJobSource source)
    {
        try
        {
            var jobs = await source.FetchJobsAsync();
            _logger.LogDebug("Fonte {Source}: {Count} vagas retornadas", source.Name, jobs.Count());
            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar vagas na fonte {Source}", source.Name);
            return [];
        }
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
                try
                {
                    await _emailService.SendJobAlertAsync(alert.User, job, alert);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao notificar alerta {AlertId} para {Email}", alert.Id, alert.User?.Email);
                }
            }
        }
    }
}
