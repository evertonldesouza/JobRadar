using System.Net;
using System.Net.Mail;
using JobRadar.Application.Abstractions;
using JobRadar.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobRadar.Infrastructure.Services;

/// <summary>
/// Envio real via SMTP (System.Net.Mail, sem dependências externas).
/// Só é registrado quando Smtp:Host está configurado; sem SMTP, usa-se o Mock.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendJobAlertAsync(User user, Job job, JobAlert alert)
    {
        var (subject, text, html) = EmailTemplates.JobAlert(user, job, alert);
        await SendAsync(user.Email, subject, text, html);
    }

    public async Task SendPasswordResetAsync(User user, string resetUrl)
    {
        var (subject, text, html) = EmailTemplates.PasswordReset(user, resetUrl);
        await SendAsync(user.Email, subject, text, html);
    }

    private async Task SendAsync(string to, string subject, string text, string html)
    {
        var host = _configuration["Smtp:Host"]!;
        var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 587;
        var user = _configuration["Smtp:User"];
        var password = _configuration["Smtp:Password"];
        var from = _configuration["Smtp:From"] ?? user ?? "no-reply@jobradar.local";
        var fromName = _configuration["Smtp:FromName"] ?? "JobRadar";
        var enableSsl = !bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) || ssl;

        using var message = new MailMessage
        {
            From = new MailAddress(from, fromName),
            Subject = subject,
            Body = text,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(to));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            html, System.Text.Encoding.UTF8, "text/html"));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Timeout = 15_000
        };
        if (!string.IsNullOrWhiteSpace(user))
            client.Credentials = new NetworkCredential(user, password);

        await client.SendMailAsync(message);
        _logger.LogInformation("Email enviado via SMTP para {To} (assunto: {Subject})", MaskEmail(to), subject);
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        return at <= 1 ? "***" : $"{email[0]}***{email[at..]}";
    }
}
