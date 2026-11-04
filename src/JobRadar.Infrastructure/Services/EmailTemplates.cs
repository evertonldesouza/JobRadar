using System.Net;
using JobRadar.Domain.Entities;

namespace JobRadar.Infrastructure.Services;

/// <summary>Templates de email (assunto, texto puro e HTML) — puros e testáveis.</summary>
public static class EmailTemplates
{
    public static (string Subject, string Text, string Html) JobAlert(User user, Job job, JobAlert alert)
    {
        var subject = $"Nova vaga: {job.Title} na {job.Company}";
        var text =
            $"Olá, {user.Name}!\n\n" +
            $"Uma nova vaga corresponde ao seu alerta no JobRadar:\n\n" +
            $"{job.Title}\n{job.Company} · {job.Location}\nPublicada em {job.PublishedAt:dd/MM/yyyy}\n\n" +
            $"Ver vaga: {job.Url}\n\n— JobRadar";

        var html = $$"""
            <div style="font-family:Arial,Helvetica,sans-serif;background:#0d1117;color:#e8eaf0;padding:24px">
              <div style="max-width:560px;margin:0 auto;background:#141a24;border-radius:16px;padding:24px">
                <p style="color:#10d9a0;font-size:12px;letter-spacing:.1em;text-transform:uppercase;margin:0 0 8px">JobRadar · nova vaga para você</p>
                <h1 style="font-size:22px;margin:0 0 8px">{{Enc(job.Title)}}</h1>
                <p style="color:#9aa7c2;margin:0 0 4px">{{Enc(job.Company)}} · {{Enc(job.Location)}}</p>
                <p style="color:#9aa7c2;margin:0 0 20px">Publicada em {{job.PublishedAt:dd/MM/yyyy}} via {{Enc(job.Source)}}</p>
                <a href="{{Enc(job.Url)}}" style="display:inline-block;background:#8b5cf6;color:#fff;text-decoration:none;padding:12px 20px;border-radius:10px">Ver a vaga</a>
                <p style="color:#5b6a8a;font-size:12px;margin:20px 0 0">Você recebeu este email porque criou um alerta no JobRadar.</p>
              </div>
            </div>
            """;

        return (subject, text, html);
    }

    public static (string Subject, string Text, string Html) PasswordReset(User user, string resetUrl)
    {
        const string subject = "Recuperação de senha — JobRadar";
        var text =
            $"Olá, {user.Name}!\n\n" +
            $"Recebemos um pedido para redefinir sua senha no JobRadar.\n" +
            $"Use o link abaixo (válido por 1 hora):\n\n{resetUrl}\n\n" +
            $"Se não foi você, ignore este email — sua senha continua a mesma.\n\n— JobRadar";

        var html = $$"""
            <div style="font-family:Arial,Helvetica,sans-serif;background:#0d1117;color:#e8eaf0;padding:24px">
              <div style="max-width:560px;margin:0 auto;background:#141a24;border-radius:16px;padding:24px">
                <p style="color:#10d9a0;font-size:12px;letter-spacing:.1em;text-transform:uppercase;margin:0 0 8px">JobRadar</p>
                <h1 style="font-size:22px;margin:0 0 8px">Redefina sua senha</h1>
                <p style="color:#9aa7c2;margin:0 0 20px">Olá, {{Enc(user.Name)}}. Use o botão abaixo — o link é válido por 1 hora.</p>
                <a href="{{Enc(resetUrl)}}" style="display:inline-block;background:#8b5cf6;color:#fff;text-decoration:none;padding:12px 20px;border-radius:10px">Criar nova senha</a>
                <p style="color:#5b6a8a;font-size:12px;margin:20px 0 0">Se não foi você, ignore este email — sua senha continua a mesma.</p>
              </div>
            </div>
            """;

        return (subject, text, html);
    }

    private static string Enc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
