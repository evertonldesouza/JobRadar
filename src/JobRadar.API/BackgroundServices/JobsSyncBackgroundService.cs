using JobRadar.Application.UseCases.Jobs;
using MediatR;

namespace JobRadar.API.BackgroundServices;

/// <summary>
/// Sincroniza as fontes periodicamente (intervalo configurável em <c>Sync:*</c>)
/// e garante que a primeira tentativa aconteça logo na subida.
/// </summary>
public sealed class JobsSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobsSyncBackgroundService> _logger;
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _interval;
    private readonly int _retentionDays;

    public JobsSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<JobsSyncBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var initialDelayMinutes = configuration.GetValue("Sync:InitialDelayMinutes", 1d);
        var intervalHours = configuration.GetValue("Sync:IntervalHours", 1d);
        _retentionDays = configuration.GetValue("Sync:RetentionDays", 90);
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
            await RunSyncAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunSyncAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var count = await mediator.Send(new SyncJobsCommand(_retentionDays), stoppingToken);
            _logger.LogInformation("Background sync concluído: {Count} novas vagas", count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no background sync");
        }
    }
}

/// <summary>
/// Dispara uma única sincronização logo após a subida, sem bloquear o start da API.
/// No plano gratuito do Render a instância hiberna; ao acordar com uma requisição,
/// este sync garante que a base receba dados novos sem esperar o ciclo periódico.
/// </summary>
public sealed class StartupSyncService : BackgroundService
{
    private static readonly TimeSpan DelayAfterStartup = TimeSpan.FromSeconds(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StartupSyncService> _logger;
    private readonly int _retentionDays;

    public StartupSyncService(
        IServiceScopeFactory scopeFactory,
        ILogger<StartupSyncService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _retentionDays = configuration.GetValue("Sync:RetentionDays", 90);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(DelayAfterStartup, stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var count = await mediator.Send(new SyncJobsCommand(_retentionDays), stoppingToken);

            _logger.LogInformation("Sync de inicialização concluído: {Count} novas vagas", count);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Desligamento normal da aplicação.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sync de inicialização falhou (o sync periódico continua ativo)");
        }
    }
}
