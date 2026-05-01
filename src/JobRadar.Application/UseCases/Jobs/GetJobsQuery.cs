using JobRadar.Application.DTOs;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Jobs;

public record GetJobsQuery(string? Technology, string? Location) : IRequest<IEnumerable<JobDto>>;

public class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, IEnumerable<JobDto>>
{
    private readonly IJobRepository _jobRepository;

    public GetJobsQueryHandler(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<IEnumerable<JobDto>> Handle(GetJobsQuery request, CancellationToken cancellationToken)
    {
        var jobs = await _jobRepository.GetAllAsync(request.Technology, request.Location);

        return jobs.Select(j => new JobDto(
            j.Id,
            j.Title,
            j.Company,
            j.Location,
            j.Url,
            j.PublishedAt,
            j.Technologies,
            j.Source
        ));
    }
}