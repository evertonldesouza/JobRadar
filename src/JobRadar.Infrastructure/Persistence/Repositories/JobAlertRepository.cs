using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobRadar.Infrastructure.Persistence.Repositories;

public class JobAlertRepository : IJobAlertRepository
{
    private readonly AppDbContext _context;

    public JobAlertRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<JobAlert>> GetByUserIdAsync(Guid userId) =>
        await _context.JobAlerts.Where(a => a.UserId == userId).ToListAsync();

    public async Task<List<JobAlert>> GetAllAsync() =>
        await _context.JobAlerts.Include(a => a.User).ToListAsync();

    public async Task<JobAlert?> GetByIdAsync(Guid id) =>
        await _context.JobAlerts.FindAsync(id);

    public async Task AddAsync(JobAlert alert)
    {
        await _context.JobAlerts.AddAsync(alert);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(JobAlert alert)
    {
        _context.JobAlerts.Remove(alert);
        await _context.SaveChangesAsync();
    }
}
