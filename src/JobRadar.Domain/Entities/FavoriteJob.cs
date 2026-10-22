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
        if (userId == Guid.Empty)
            throw new ArgumentException("Usuário é obrigatório.", nameof(userId));
        if (jobId == Guid.Empty)
            throw new ArgumentException("Vaga é obrigatória.", nameof(jobId));

        return new FavoriteJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            JobId = jobId,
            SavedAt = DateTime.UtcNow
        };
    }
}
