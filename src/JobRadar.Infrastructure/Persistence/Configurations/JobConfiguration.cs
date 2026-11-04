using JobRadar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        var comparer = new ValueComparer<List<string>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList()
        );

        builder.Property(j => j.Technologies)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            )
            .Metadata.SetValueComparer(comparer);

        builder.HasIndex(j => j.Url).IsUnique();
    }
}