using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Configurations;

public sealed class ServiceOrderDiagnosisConfiguration
    : IEntityTypeConfiguration<ServiceOrderDiagnosisEntity>
{
    public void Configure(
        EntityTypeBuilder<ServiceOrderDiagnosisEntity> builder)
    {
        builder.ToTable("ServiceOrderDiagnoses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.InternalNotes)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasOne(x => x.ServiceOrder)
            .WithOne(x => x.Diagnosis)
            .HasForeignKey<ServiceOrderDiagnosisEntity>(
                x => x.ServiceOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}