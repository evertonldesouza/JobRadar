using JobRadar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobRadar.Infrastructure.Persistence.Configurations;

public class JobAlertConfiguration : IEntityTypeConfiguration<JobAlert>
{
    public void Configure(EntityTypeBuilder<JobAlert> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Technology).HasMaxLength(100);
        builder.Property(a => a.Location).HasMaxLength(200);
        builder.Property(a => a.Source).HasMaxLength(50);

        builder.HasOne(a => a.User)
            .WithMany(u => u.Alerts)
            .HasForeignKey(a => a.UserId);
    }
}
