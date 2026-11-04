using JobRadar.Application.DTOs;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Alerts;

public record GetAlertsQuery(Guid UserId) : IRequest<List<AlertDto>>;

public sealed class GetAlertsQueryHandler : IRequestHandler<GetAlertsQuery, List<AlertDto>>
{
    private readonly IJobAlertRepository _alertRepository;

    public GetAlertsQueryHandler(IJobAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task<List<AlertDto>> Handle(GetAlertsQuery request, CancellationToken cancellationToken)
    {
        var alerts = await _alertRepository.GetByUserIdAsync(request.UserId);
        return alerts.Select(a => new AlertDto(a.Id, a.UserId, a.Technology, a.Location, a.Source, a.CreatedAt)).ToList();
    }
}
