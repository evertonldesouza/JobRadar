using JobRadar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobRadar.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Title).IsRequired().HasMaxLength(300);
        builder.Property(j => j.Company).IsRequired().HasMaxLength(200);
        builder.Property(j => j.Location).HasMaxLength(200);
        builder.Property(j => j.Url).IsRequired().HasMaxLength(1000);
        builder.Property(j => j.Source).IsRequired().HasMaxLength(100);
        builder.Property(j => j.Technologies)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            );
        builder.HasIndex(j => j.Url).IsUnique();
    }
}