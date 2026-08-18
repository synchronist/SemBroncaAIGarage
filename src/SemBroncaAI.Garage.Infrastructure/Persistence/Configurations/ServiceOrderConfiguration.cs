using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Configurations;

public sealed class ServiceOrderConfiguration
    : IEntityTypeConfiguration<ServiceOrderEntity>
{
    public void Configure(EntityTypeBuilder<ServiceOrderEntity> builder)
    {
        builder.ToTable("ServiceOrders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Number)
            .IsRequired();

        builder.Property(x => x.CustomerComplaint)
            .HasMaxLength(FieldLengthLimits.CustomerComplaint)
            .IsRequired();

        builder.Property(x => x.Mileage)
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ArchivedAt)
            .IsRequired(false);

        builder.Property(x => x.Version)
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(x => new { x.GarageId, x.ArchivedAt });

        builder.HasIndex(x => new { x.GarageId, x.Number })
            .IsUnique()
            .HasDatabaseName(DatabaseConstraintNames.UniqueServiceOrderNumberPerGarage);

        builder.HasOne(x => x.Garage)
            .WithMany(x => x.ServiceOrders)
            .HasForeignKey(x => x.GarageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany(x => x.ServiceOrders)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.History)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.EstimateApprovals)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
