using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Alerts;

public record DeleteAlertCommand(Guid Id, Guid UserId) : IRequest;

public class DeleteAlertCommandHandler : IRequestHandler<DeleteAlertCommand>
{
    private readonly IJobAlertRepository _alertRepository;

    public DeleteAlertCommandHandler(IJobAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task Handle(DeleteAlertCommand request, CancellationToken cancellationToken)
    {
        var alert = await _alertRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException("Alerta não encontrado.");

        if (alert.UserId != request.UserId)
            throw new UnauthorizedAccessException("Este alerta não pertence ao usuário.");

        await _alertRepository.RemoveAsync(alert);
    }
}
