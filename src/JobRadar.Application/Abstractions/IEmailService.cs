using JobRadar.Domain.Entities;

namespace JobRadar.Application.Abstractions;

public interface IEmailService
{
    Task SendJobAlertAsync(User user, Job job, JobAlert alert);

    /// <summary>Envia o link de recuperação de senha (token em texto puro, válido por tempo limitado).</summary>
    Task SendPasswordResetAsync(User user, string resetUrl);
}
