using JobRadar.Application.DTOs;
using JobRadar.Domain.Interfaces;
using MediatR;

namespace JobRadar.Application.UseCases.Jobs;

public record GetJobsQuery(
    string? Technology,
    string? Location,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<JobDto>>;

public class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, PagedResult<JobDto>>
{
    private readonly IJobRepository _jobRepository;

    public GetJobsQueryHandler(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<PagedResult<JobDto>> Handle(GetJobsQuery request, CancellationToken cancellationToken)
    {
        var (jobs, totalCount) = await _jobRepository.GetAllAsync(
            request.Technology,
            request.Location,
            request.Page,
            request.PageSize
        );

        var items = jobs.Select(j => new JobDto(
            j.Id, j.Title, j.Company, j.Location,
            j.Url, j.PublishedAt, j.Technologies, j.Source
        ));

        return new PagedResult<JobDto>(items, request.Page, request.PageSize, totalCount);
    }
}