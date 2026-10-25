namespace JobRadar.Application.Common;

/// <summary>
/// Monta o redirect pós-login (Google OAuth) para o frontend.
/// Os tokens vão no fragmento da URL (#...): nunca chegam a servidores/logs
/// e o frontend os remove do histórico logo após ler.
/// Só redireciona para a base configurada (Frontend:BaseUrl) — nunca para
/// URLs fornecidas pelo usuário (sem open-redirect).
/// </summary>
public static class FrontendRedirect
{
    public static string? BuildAuthRedirect(string? frontendBaseUrl, string? accessToken, string? refreshToken = null, string? error = null)
    {
        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            return null;

        if (!Uri.TryCreate(frontendBaseUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;

        var baseUrl = frontendBaseUrl.Trim().TrimEnd('/') + "/";

        if (!string.IsNullOrWhiteSpace(error))
            return $"{baseUrl}#authError={Uri.EscapeDataString(error)}";

        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        var fragment = $"token={Uri.EscapeDataString(accessToken)}";
        if (!string.IsNullOrWhiteSpace(refreshToken))
            fragment += $"&refreshToken={Uri.EscapeDataString(refreshToken)}";

        return $"{baseUrl}#{fragment}";
    }

    /// <summary>Link de recuperação de senha enviado por email (token no fragmento).</summary>
    public static string? BuildPasswordResetUrl(string? frontendBaseUrl, string resetToken)
    {
        if (string.IsNullOrWhiteSpace(frontendBaseUrl) || string.IsNullOrWhiteSpace(resetToken))
            return null;

        if (!Uri.TryCreate(frontendBaseUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;

        var baseUrl = frontendBaseUrl.Trim().TrimEnd('/') + "/";
        return $"{baseUrl}#resetToken={Uri.EscapeDataString(resetToken)}";
    }
}
