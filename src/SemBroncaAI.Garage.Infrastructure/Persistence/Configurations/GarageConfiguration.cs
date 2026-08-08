using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemBroncaAI.Garage.Domain.Entities.Garage;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Configurations;

public sealed class GarageConfiguration : IEntityTypeConfiguration<GarageEntity>
{
    public void Configure(EntityTypeBuilder<GarageEntity> builder)
    {
        builder.ToTable("Garages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Document)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasMaxLength(20);

        builder.Property(x => x.Email)
            .HasMaxLength(150);

        builder.Property(x => x.PostalCode).HasMaxLength(10);
        builder.Property(x => x.Street).HasMaxLength(200);
        builder.Property(x => x.Number).HasMaxLength(20);
        builder.Property(x => x.Complement).HasMaxLength(100);
        builder.Property(x => x.Neighborhood).HasMaxLength(100);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.State).HasMaxLength(2);

        builder.Property(x => x.Active)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.Document)
            .IsUnique();
    }
}
