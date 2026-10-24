using Microsoft.Extensions.Logging;

namespace JobRadar.Infrastructure.Http;

/// <summary>
/// Resiliência para as fontes externas de vagas, sem dependências novas:
/// até 3 tentativas com backoff exponencial (200ms, 400ms) para falhas
/// transitórias (5xx, 429, rede/timeout). Requisições são GET simples,
/// então a mensagem é clonada a cada tentativa.
/// </summary>
public sealed class ResilientRetryHandler : DelegatingHandler
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan BaseDelay = TimeSpan.FromMilliseconds(200);

    private readonly ILogger<ResilientRetryHandler> _logger;

    public ResilientRetryHandler(ILogger<ResilientRetryHandler> logger) => _logger = logger;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            HttpResponseMessage? response = null;
            try
            {
                response = await base.SendAsync(Clone(request), cancellationToken);

                if (!IsTransient(response.StatusCode) || attempt == MaxAttempts)
                    return response;

                _logger.LogWarning(
                    "Fonte externa respondeu {StatusCode} (tentativa {Attempt}/{Max}); repetindo...",
                    (int)response.StatusCode, attempt, MaxAttempts);
                response.Dispose();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && attempt < MaxAttempts)
            {
                _logger.LogWarning(ex,
                    "Falha transitória ao chamar fonte externa (tentativa {Attempt}/{Max}); repetindo...",
                    attempt, MaxAttempts);
                response?.Dispose();
            }

            await Task.Delay(BaseDelay * Math.Pow(2, attempt - 1), cancellationToken);
        }
    }

    private static bool IsTransient(System.Net.HttpStatusCode statusCode) =>
        statusCode == System.Net.HttpStatusCode.TooManyRequests || (int)statusCode >= 500;

    private static HttpRequestMessage Clone(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        return clone;
    }
}
