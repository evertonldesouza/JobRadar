using JobRadar.Application.UseCases.Jobs;
using MediatR;

namespace JobRadar.API.BackgroundServices;

/// <summary>Sincroniza as fontes periodicamente (intervalo configurável em <c>Sync:*</c>).</summary>
public sealed class JobsSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobsSyncBackgroundService> _logger;
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _interval;

    public JobsSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<JobsSyncBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var initialDelayMinutes = configuration.GetValue("Sync:InitialDelayMinutes", 1d);
        var intervalHours = configuration.GetValue("Sync:IntervalHours", 1d);
        _initialDelay = TimeSpan.FromMinutes(Math.Max(initialDelayMinutes, 0));
        _interval = TimeSpan.FromHours(Math.Max(intervalHours, 0.05));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Background sync iniciado (primeira execução em {InitialDelay}, depois a cada {Interval})",
            _initialDelay, _interval);

        if (_initialDelay > TimeSpan.Zero)
            await Task.Delay(_initialDelay, stoppingToken);

        using var timer = new PeriodicTimer(_interval);
        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var count = await mediator.Send(new SyncJobsCommand(), stoppingToken);
                _logger.LogInformation("Background sync concluído: {Count} novas vagas", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no background sync");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
