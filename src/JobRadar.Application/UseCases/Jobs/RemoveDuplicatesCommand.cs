using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Jobs;

public record RemoveDuplicatesCommand : IRequest<int>;

public class RemoveDuplicatesCommandHandler : IRequestHandler<RemoveDuplicatesCommand, int>
{
    private readonly IJobRepository _jobRepository;

    public RemoveDuplicatesCommandHandler(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<int> Handle(RemoveDuplicatesCommand request, CancellationToken cancellationToken)
    {
        return await _jobRepository.RemoveDuplicatesAsync();
    }
}