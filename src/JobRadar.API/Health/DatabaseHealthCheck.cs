using JobRadar.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JobRadar.API.Health;

/// <summary>Readiness: verifica se a API consegue falar com o PostgreSQL.</summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    public DatabaseHealthCheck(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Banco de dados acessível.")
                : HealthCheckResult.Unhealthy("Não foi possível conectar ao banco de dados.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Falha ao verificar o banco de dados.", ex);
        }
    }
}
