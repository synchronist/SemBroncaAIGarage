using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;
using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<VehicleEntity>
{
    public void Configure(EntityTypeBuilder<VehicleEntity> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Plate)
            .HasMaxLength(FieldLengthLimits.VehiclePlate)
            .IsRequired();

        builder.Property(v => v.Brand)
            .HasMaxLength(FieldLengthLimits.VehicleBrand)
            .IsRequired();

        builder.Property(v => v.Model)
            .HasMaxLength(FieldLengthLimits.VehicleModel)
            .IsRequired();

        builder.Property(v => v.Version)
            .HasMaxLength(FieldLengthLimits.VehicleVersion);

        builder.Property(v => v.Color)
            .HasMaxLength(FieldLengthLimits.VehicleColor);

        builder.Property(v => v.Fuel)
            .HasMaxLength(FieldLengthLimits.VehicleFuel);

        builder.Property(v => v.Year)
            .IsRequired();

        builder.Property(v => v.Mileage)
            .IsRequired();

        builder.Property(v => v.Active)
            .IsRequired();

        builder.Property(v => v.CreatedAt)
            .IsRequired();

        builder.HasIndex(v => new { v.GarageId, v.Plate }).IsUnique();

        builder.HasOne(v => v.Garage)
            .WithMany(g => g.Vehicles)
            .HasForeignKey(v => v.GarageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Customer)
            .WithMany(c => c.Vehicles)
            .HasForeignKey(v => v.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
