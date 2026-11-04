using JobRadar.Application.DTOs;
using JobRadar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace JobRadar.Application.UseCases.Jobs;

public static class JobsCache
{
    private static int _version;

    public static int Version => Volatile.Read(ref _version);

    public static void Invalidate() => Interlocked.Increment(ref _version);
}

public record GetJobsQuery(
    string? Technology,
    string? Location,
    string? Source,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<JobDto>>;

public sealed class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, PagedResult<JobDto>>
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
        // Chave normalizada (trim + minúsculas) para aproveitar cache entre variações de caixa.
        var cacheKey = string.Join('|',
            "jobs_v" + JobsCache.Version,
            request.Technology?.Trim().ToLowerInvariant(),
            request.Location?.Trim().ToLowerInvariant(),
            request.Source?.Trim().ToLowerInvariant(),
            request.Page,
            request.PageSize);

        if (_cache.TryGetValue(cacheKey, out PagedResult<JobDto>? cached))
            return cached!;

        var (jobs, totalCount) = await _jobRepository.GetAllAsync(
            request.Technology,
            request.Location,
            request.Source,
            request.Page,
            request.PageSize
        );

        var items = jobs.Select(JobDto.FromJob).ToList();

        var result = new PagedResult<JobDto>(items, request.Page, request.PageSize, totalCount);

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

        return result;
    }
}
