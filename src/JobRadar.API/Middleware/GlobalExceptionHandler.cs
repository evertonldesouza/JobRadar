using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace JobRadar.API.Middleware;

/// <summary>
/// Tratamento global de exceções no formato RFC 7807 (ProblemDetails),
/// sem vazar detalhes internos em produção.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidationException => (HttpStatusCode.BadRequest, "Falha de validação."),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Acesso não autorizado."),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Recurso não encontrado."),
            InvalidOperationException => (HttpStatusCode.Conflict, "Operação inválida."),
            ArgumentException => (HttpStatusCode.BadRequest, "Requisição inválida."),
            _ => (HttpStatusCode.InternalServerError, "Ocorreu um erro interno.")
        };

        if (status == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Erro interno não tratado em {Method} {Path}", httpContext.Request.Method, Sanitize(httpContext.Request.Path.Value));
        else
            _logger.LogWarning(exception, "{Title} em {Method} {Path}", title, httpContext.Request.Method, Sanitize(httpContext.Request.Path.Value));

        var problem = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Detail = status == HttpStatusCode.InternalServerError && !_environment.IsDevelopment()
                ? "Ocorreu um erro interno. Tente novamente mais tarde."
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (exception is ValidationException validationException)
        {
            problem.Extensions["errors"] = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        httpContext.Response.StatusCode = (int)status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    /// <summary>Remove quebras de linha de valores da requisição antes de logar (log injection).</summary>
    private static string Sanitize(string? value) =>
        (value ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
}
