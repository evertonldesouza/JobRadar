namespace JobRadar.Domain.Entities;

public class FavoriteJob
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid JobId { get; private set; }
    public DateTime SavedAt { get; private set; }
    public Job Job { get; private set; } = null!;

    protected FavoriteJob() { }

    public static FavoriteJob Create(Guid userId, Guid jobId)
    {
        return new FavoriteJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            JobId = jobId,
            SavedAt = DateTime.UtcNow
        };
    }
}