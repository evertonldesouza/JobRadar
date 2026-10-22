using JobRadar.Application.DTOs;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Alerts;

public record CreateAlertCommand(Guid UserId, string? Technology, string? Location, string? Source) : IRequest<AlertDto>;

public sealed class CreateAlertCommandHandler : IRequestHandler<CreateAlertCommand, AlertDto>
{
    private readonly IJobAlertRepository _alertRepository;
    private readonly IUserRepository _userRepository;

    public CreateAlertCommandHandler(IJobAlertRepository alertRepository, IUserRepository userRepository)
    {
        _alertRepository = alertRepository;
        _userRepository = userRepository;
    }

    public async Task<AlertDto> Handle(CreateAlertCommand request, CancellationToken cancellationToken)
    {
        _ = await _userRepository.GetByIdAsync(request.UserId)
            ?? throw new KeyNotFoundException("Usuário não encontrado.");

        var alert = JobAlert.Create(request.UserId, request.Technology, request.Location, request.Source);
        await _alertRepository.AddAsync(alert);
        return new AlertDto(alert.Id, alert.UserId, alert.Technology, alert.Location, alert.Source, alert.CreatedAt);
    }
}
