using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Configurations;

public sealed class ServiceOrderEstimateItemConfiguration : IEntityTypeConfiguration<ServiceOrderEstimateItemEntity>
{
    public void Configure(EntityTypeBuilder<ServiceOrderEstimateItemEntity> builder)
    {
        builder.ToTable("ServiceOrderEstimateItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Description).HasMaxLength(FieldLengthLimits.EstimateItemDescription).IsRequired();
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(12, 3).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(12, 2).IsRequired();
        builder.Ignore(x => x.Total);

        builder.HasOne<ServiceOrderEstimateEntity>()
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.EstimateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
