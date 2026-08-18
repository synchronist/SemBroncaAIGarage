using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemBroncaAI.Garage.Domain.Entities.Garage;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Configurations;

public sealed class GarageSubscriptionConfiguration : IEntityTypeConfiguration<GarageSubscriptionEntity>
{
    public void Configure(EntityTypeBuilder<GarageSubscriptionEntity> builder)
    {
        builder.ToTable("GarageSubscriptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Plan).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.GarageId).IsUnique();
        builder.HasOne<GarageEntity>().WithOne().HasForeignKey<GarageSubscriptionEntity>(x => x.GarageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
