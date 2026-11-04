using JobRadar.Application.DTOs;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Jobs;

/// <summary>Detalhe de uma vaga — usado pela página de detalhes do frontend.</summary>
public record GetJobByIdQuery(Guid Id) : IRequest<JobDto>;

public sealed class GetJobByIdQueryHandler : IRequestHandler<GetJobByIdQuery, JobDto>
{
    private readonly IJobRepository _jobRepository;

    public GetJobByIdQueryHandler(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<JobDto> Handle(GetJobByIdQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException("Vaga não encontrada ou já removida.");

        return JobDto.FromJob(job);
    }
}
