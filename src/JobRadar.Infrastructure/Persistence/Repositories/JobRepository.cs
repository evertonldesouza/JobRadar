using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobRadar.Infrastructure.Persistence.Repositories;

public class JobRepository : IJobRepository
{
    private readonly AppDbContext _context;

    public JobRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Job> Jobs, int TotalCount)> GetAllAsync(
        string? technology = null,
        string? location = null,
        string? source = null,
        int page = 1,
        int pageSize = 20)
    {
        // Defesa em profundidade: a validação principal ocorre no pipeline (FluentValidation).
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Jobs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(technology))
        {
            // Busca tolerante: aceita o termo como digitado e em minúsculas, e também
            // procura no título/empresa (vagas cujo anúncio não traz tags).
            var term = technology.Trim();
            var lower = term.ToLowerInvariant();

            query = query.Where(j =>
                j.Technologies.Contains(term) ||
                j.Technologies.Contains(lower) ||
                j.Title.ToLower().Contains(lower) ||
                j.Company.ToLower().Contains(lower));
        }

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(j => j.Location.ToLower().Contains(location.Trim().ToLower()));

        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(j => j.Source.ToLower() == source.Trim().ToLower());

        var totalCount = await query.CountAsync();

        var jobs = await query
            .OrderByDescending(j => j.PublishedAt)
            .ThenBy(j => j.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (jobs, totalCount);
    }

    public async Task<Job?> GetByIdAsync(Guid id) =>
        await _context.Jobs.FindAsync(id);

    public async Task<bool> ExistsByUrlAsync(string url) =>
        await _context.Jobs.AnyAsync(j => j.Url == url);

    public async Task<bool> ExistsByTitleAndCompanyAsync(string title, string company) =>
        await _context.Jobs.AnyAsync(j => j.Title == title && j.Company == company);

    public async Task AddRangeAsync(IEnumerable<Job> jobs)
    {
        await _context.Jobs.AddRangeAsync(jobs);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Guid>> GetExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return [];

        return await _context.Jobs
            .AsNoTracking()
            .Where(j => ids.Contains(j.Id))
            .Select(j => j.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetExistingUrlsAsync(IReadOnlyCollection<string> urls, CancellationToken cancellationToken = default)
    {
        if (urls.Count == 0)
            return [];

        return await _context.Jobs
            .AsNoTracking()
            .Where(j => urls.Contains(j.Url))
            .Select(j => j.Url)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetExistingDeduplicationKeysAsync(IReadOnlyCollection<string> titles, CancellationToken cancellationToken = default)
    {
        if (titles.Count == 0)
            return [];

        var matches = await _context.Jobs
            .AsNoTracking()
            .Where(j => titles.Contains(j.Title))
            .Select(j => new { j.Title, j.Company })
            .ToListAsync(cancellationToken);

        return matches
            .Select(j => $"{j.Title.Trim().ToLowerInvariant()}|{j.Company.Trim().ToLowerInvariant()}")
            .ToList();
    }

    public async Task<int> RemoveDuplicatesAsync()
    {
        var allJobs = await _context.Jobs
            .OrderBy(j => j.CreatedAt)
            .ToListAsync();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var toRemove = new List<Job>();

        foreach (var job in allJobs)
        {
            if (!seen.Add(job.DeduplicationKey))
                toRemove.Add(job);
        }

        if (toRemove.Count == 0)
            return 0;

        _context.Jobs.RemoveRange(toRemove);
        await _context.SaveChangesAsync();
        return toRemove.Count;
    }
}
