using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace JobRadar.Application.Behaviors;

/// <summary>Registra duração e falhas de cada request MediatR (sem logar o payload, que pode conter senhas).</summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const long SlowRequestThresholdMs = 3_000;
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds >= SlowRequestThresholdMs)
                _logger.LogWarning("{Request} concluído em {ElapsedMs} ms (lento)", requestName, stopwatch.ElapsedMilliseconds);
            else
                _logger.LogDebug("{Request} concluído em {ElapsedMs} ms", requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "{Request} falhou após {ElapsedMs} ms", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
