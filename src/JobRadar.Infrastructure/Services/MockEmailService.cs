using JobRadar.Application.Abstractions;
using JobRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JobRadar.Infrastructure.Services;

public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendJobAlertAsync(User user, Job job, JobAlert alert)
    {
        _logger.LogInformation(
            " [MockEmail] Para {Email} - Vaga '{Title}' em {Company} ({Location}) " +
            "corresponde ao alerta (Tech: {Tech}, Local: {Loc}, Fonte: {Source})",
            user.Email, job.Title, job.Company, job.Location,
            alert.Technology ?? "*", alert.Location ?? "*", alert.Source ?? "*");

        return Task.CompletedTask;
    }
}
