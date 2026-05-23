using JobRadar.Application.DTOs;
using JobRadar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace JobRadar.Application.UseCases.Jobs;

public static class JobsCache
{
    public static int Version { get; set; } = 0;
}

public record GetJobsQuery(
    string? Technology,
    string? Location,
    string? Source,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<JobDto>>;

public class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, PagedResult<JobDto>>
{
    private readonly IJobRepository _jobRepository;
    private readonly IMemoryCache _cache;

    public GetJobsQueryHandler(IJobRepository jobRepository, IMemoryCache cache)
    {
        _jobRepository = jobRepository;
        _cache = cache;
    }

    public async Task<PagedResult<JobDto>> Handle(GetJobsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"jobs_v{JobsCache.Version}_{request.Technology}_{request.Location}_{request.Source}_{request.Page}_{request.PageSize}";

        if (_cache.TryGetValue(cacheKey, out PagedResult<JobDto>? cached))
            return cached!;

        var (jobs, totalCount) = await _jobRepository.GetAllAsync(
            request.Technology,
            request.Location,
            request.Source,
            request.Page,
            request.PageSize
        );

        var items = jobs.Select(j => new JobDto(
            j.Id, j.Title, j.Company, j.Location,
            j.Url, j.PublishedAt, j.Technologies, j.Source
        ));

        var result = new PagedResult<JobDto>(items, request.Page, request.PageSize, totalCount);

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

        return result;
    }
}