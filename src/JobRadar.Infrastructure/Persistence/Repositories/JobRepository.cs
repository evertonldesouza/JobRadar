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

    public async Task<IEnumerable<Job>> GetAllAsync(string? technology = null, string? location = null)
    {
        var query = _context.Jobs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(technology))
            query = query.Where(j => j.Technologies.Contains(technology));

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(j => j.Location.Contains(location));

        return await query.OrderByDescending(j => j.PublishedAt).ToListAsync();
    }

    public async Task<Job?> GetByIdAsync(Guid id) =>
        await _context.Jobs.FindAsync(id);

    public async Task<bool> ExistsByUrlAsync(string url) =>
        await _context.Jobs.AnyAsync(j => j.Url == url);

    public async Task AddRangeAsync(IEnumerable<Job> jobs)
    {
        await _context.Jobs.AddRangeAsync(jobs);
        await _context.SaveChangesAsync();
    }
}