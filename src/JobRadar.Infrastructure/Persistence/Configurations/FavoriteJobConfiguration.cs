using JobRadar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobRadar.Infrastructure.Persistence.Configurations;

public class FavoriteJobConfiguration : IEntityTypeConfiguration<FavoriteJob>
{
    public void Configure(EntityTypeBuilder<FavoriteJob> builder)
    {
        builder.HasKey(f => f.Id);
        builder.HasIndex(f => new { f.UserId, f.JobId }).IsUnique();
        builder.HasOne(f => f.Job)
            .WithMany()
            .HasForeignKey(f => f.JobId);
    }
}