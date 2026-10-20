using System.Diagnostics;

namespace JobRadar.API.Middleware;

/// <summary>Log estruturado por requisição + propagação de correlation id.</summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = context.TraceIdentifier;

        context.Response.Headers["X-Correlation-Id"] = correlationId;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var isHealth = context.Request.Path.StartsWithSegments("/health");
            var level = isHealth ? LogLevel.Debug : LogLevel.Information;
            _logger.Log(level,
                "HTTP {Method} {Path} respondeu {StatusCode} em {ElapsedMs} ms (CorrelationId: {CorrelationId})",
                context.Request.Method, context.Request.Path, context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds, correlationId);
        }
    }
}
