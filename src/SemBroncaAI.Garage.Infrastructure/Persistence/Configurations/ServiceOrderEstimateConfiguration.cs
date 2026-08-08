using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Configurations;

public sealed class ServiceOrderEstimateConfiguration : IEntityTypeConfiguration<ServiceOrderEstimateEntity>
{
    public void Configure(EntityTypeBuilder<ServiceOrderEstimateEntity> builder)
    {
        builder.ToTable("ServiceOrderEstimates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.Ignore(x => x.ServicesSubtotal);
        builder.Ignore(x => x.PartsSubtotal);
        builder.Ignore(x => x.Total);
        builder.Ignore(x => x.IsValid);

        builder.HasOne<ServiceOrderEntity>()
            .WithOne(x => x.Estimate)
            .HasForeignKey<ServiceOrderEstimateEntity>(x => x.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
