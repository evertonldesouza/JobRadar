using JobRadar.Application.Abstractions;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using JobRadar.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace JobRadar.Application.UseCases.Jobs;

/// <param name="RetentionDays">
/// Vagas publicadas há mais tempo que isso são removidas no fim do sync.
/// 0 (padrão) desativa a expiração.
/// </param>
public record SyncJobsCommand(int RetentionDays = 0) : IRequest<int>;

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
            await NotifyAlertsAsync(newJobs);
        }

        // Backfill: vagas já importadas antes de a fonte informar salário (ou antes
        // de existir o campo) recebem a faixa agora. Sem isso, o histórico nunca é
        // completado — o dedupe por URL faria essas vagas serem descartadas sempre.
        var updated = await BackfillSalariesAsync(candidates, existingUrls, cancellationToken);

        var purged = 0;
        if (request.RetentionDays > 0)
        {
            var cutoff = DateTime.UtcNow.AddDays(-request.RetentionDays);
            purged = await _jobRepository.RemoveOlderThanAsync(cutoff, cancellationToken);

            if (purged > 0)
                _logger.LogInformation(
                    "Expiração aplicada: {Purged} vagas publicadas antes de {Cutoff:yyyy-MM-dd} removidas",
                    purged, cutoff);
        }

        if (newJobs.Count > 0 || updated > 0 || purged > 0)
            JobsCache.Invalidate();

        _logger.LogInformation(
            "Sync finalizado: {New} novas vagas, {Updated} salários completados, {Purged} expiradas ({Candidates} avaliadas, {Sources} fontes)",
            newJobs.Count, updated, purged, candidates.Count, _sources.Count());

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

    /// <summary>
    /// Completa a faixa salarial de vagas que já estavam na base. Só escreve
    /// quando a vaga passa a informar salário que não conhecíamos antes.
    /// </summary>
    private async Task<int> BackfillSalariesAsync(
        List<Job> candidates,
        HashSet<string> existingUrls,
        CancellationToken cancellationToken)
    {
        var withSalary = candidates
            .Where(job => job.HasSalary && existingUrls.Contains(job.Url))
            .GroupBy(job => job.Url, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        if (withSalary.Count == 0)
            return 0;

        var tracked = await _jobRepository.GetByUrlsAsync(withSalary.Select(j => j.Url).ToList(), cancellationToken)
                      ?? [];

        if (tracked.Count == 0)
            return 0;

        var byUrl = withSalary.ToDictionary(job => job.Url, StringComparer.OrdinalIgnoreCase);
        var updated = 0;

        foreach (var job in tracked)
        {
            if (byUrl.TryGetValue(job.Url, out var source) &&
                job.TrySetSalary(SalaryRange.FromJob(source)))
            {
                updated++;
            }
        }

        if (updated > 0)
        {
            await _jobRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Backfill de salário concluído: {Updated} vagas atualizadas", updated);
        }

        return updated;
    }

    private async Task NotifyAlertsAsync(List<Job> newJobs)
    {
        var alerts = await _alertRepository.GetAllAsync() ?? [];

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
