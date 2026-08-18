using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Configurations;

public sealed class ServiceOrderNumberSequenceConfiguration
    : IEntityTypeConfiguration<ServiceOrderNumberSequence>
{
    public void Configure(EntityTypeBuilder<ServiceOrderNumberSequence> builder)
    {
        builder.ToTable("ServiceOrderNumberSequences");
        builder.HasKey(x => x.GarageId);
        builder.Property(x => x.GarageId).ValueGeneratedNever();
        builder.Property(x => x.LastNumber).IsRequired();
        builder.HasOne<Domain.Entities.Garage.GarageEntity>()
            .WithOne()
            .HasForeignKey<ServiceOrderNumberSequence>(x => x.GarageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
