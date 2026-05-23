using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Alerts;

public record GetAlertsQuery(Guid UserId) : IRequest<List<JobAlert>>;

public class GetAlertsQueryHandler : IRequestHandler<GetAlertsQuery, List<JobAlert>>
{
    private readonly IJobAlertRepository _alertRepository;

    public GetAlertsQueryHandler(IJobAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task<List<JobAlert>> Handle(GetAlertsQuery request, CancellationToken cancellationToken) =>
        await _alertRepository.GetByUserIdAsync(request.UserId);
}
