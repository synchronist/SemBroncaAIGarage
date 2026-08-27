using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Configurations;

public sealed class ProcessedBillingEventConfiguration : IEntityTypeConfiguration<ProcessedBillingEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedBillingEvent> builder)
    {
        builder.ToTable("ProcessedBillingEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasMaxLength(100);
        builder.Property(x => x.Type).HasMaxLength(100).IsRequired();
    }
}
