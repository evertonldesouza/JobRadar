using JobRadar.Application.UseCases.Jobs;
using MediatR;

namespace JobRadar.API.BackgroundServices;

public class JobsSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobsSyncBackgroundService> _logger;

    public JobsSyncBackgroundService(IServiceScopeFactory scopeFactory, ILogger<JobsSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background sync service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var count = await mediator.Send(new SyncJobsCommand(), stoppingToken);
                _logger.LogInformation("Background sync concluído: {Count} novas vagas", count);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no background sync");
            }
        }
    }
}
