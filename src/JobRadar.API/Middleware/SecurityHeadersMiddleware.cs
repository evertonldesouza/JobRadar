namespace JobRadar.API.Middleware;

/// <summary>Cabeçalhos de segurança para respostas da API (OWASP).</summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        // A API só devolve JSON; o CSP restritivo não se aplica à UI do Scalar (dev).
        if (context.Request.Path.StartsWithSegments("/api"))
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        await _next(context);
    }
}
