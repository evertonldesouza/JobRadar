using JobRadar.Domain.Entities;

namespace JobRadar.Application.Abstractions;

public interface IEmailService
{
    Task SendJobAlertAsync(User user, Job job, JobAlert alert);
}
