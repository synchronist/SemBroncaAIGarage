using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemBroncaAI.Garage.Domain.Entities.Garage;

namespace SemBroncaAI.Garage.Infrastructure.Identity;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Active).IsRequired();
        builder.HasIndex(x => x.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();

        builder.HasOne(x => x.Garage)
            .WithMany()
            .HasForeignKey(x => x.GarageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
