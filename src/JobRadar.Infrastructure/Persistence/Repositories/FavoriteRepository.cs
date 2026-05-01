using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobRadar.Infrastructure.Persistence.Repositories;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly AppDbContext _context;

    public FavoriteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FavoriteJob>> GetByUserIdAsync(Guid userId) =>
        await _context.FavoriteJobs
            .Include(f => f.Job)
            .Where(f => f.UserId == userId)
            .ToListAsync();

    public async Task<bool> ExistsAsync(Guid userId, Guid jobId) =>
        await _context.FavoriteJobs.AnyAsync(f => f.UserId == userId && f.JobId == jobId);

    public async Task AddAsync(FavoriteJob favorite)
    {
        await _context.FavoriteJobs.AddAsync(favorite);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Guid userId, Guid jobId)
    {
        var favorite = await _context.FavoriteJobs
            .FirstOrDefaultAsync(f => f.UserId == userId && f.JobId == jobId);

        if (favorite is not null)
        {
            _context.FavoriteJobs.Remove(favorite);
            await _context.SaveChangesAsync();
        }
    }
}