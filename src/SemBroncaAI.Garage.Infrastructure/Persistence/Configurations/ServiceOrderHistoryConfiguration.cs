using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Configurations;

public sealed class ServiceOrderHistoryConfiguration
    : IEntityTypeConfiguration<ServiceOrderHistoryEntity>
{
    public void Configure(
        EntityTypeBuilder<ServiceOrderHistoryEntity> builder)
    {
        builder.ToTable("ServiceOrderHistory");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ServiceOrderId)
            .IsRequired();

        builder.Property(x => x.PreviousStatus)
            .HasConversion<int>()
            .IsRequired(false);

        builder.Property(x => x.CurrentStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ActorId)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.ServiceOrder)
            .WithMany(x => x.History)
            .HasForeignKey(x => x.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}