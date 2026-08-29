using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Configurations;

public sealed class ServiceOrderEstimateApprovalConfiguration : IEntityTypeConfiguration<ServiceOrderEstimateApprovalEntity>
{
    public void Configure(EntityTypeBuilder<ServiceOrderEstimateApprovalEntity> builder)
    {
        builder.ToTable("ServiceOrderEstimateApprovals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.Property(x => x.ProtectedToken).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.HasIndex(x => new { x.ServiceOrderId, x.EstimateUpdatedAt })
            .IsUnique()
            .HasDatabaseName(ApprovalRequestPersistence.ActiveApprovalConstraint)
            .HasFilter("\"Status\" = 1 AND \"InvalidatedAt\" IS NULL");
        builder.Property(x => x.CustomerName).HasMaxLength(200);
        builder.Property(x => x.CustomerComment).HasMaxLength(1000);
        builder.Property(x => x.CustomerDocument).HasMaxLength(14);
        builder.Property(x => x.CustomerPhone).HasMaxLength(20);
        builder.Property(x => x.ClientIp).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.EstimateSnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ApprovedItemIdsJson).HasColumnType("jsonb");
        builder.Property(x => x.ApprovedTotal).HasPrecision(18, 2);
        builder.Property(x => x.RespondedAt).IsConcurrencyToken();
        builder.Property(x => x.EstimateTotal).HasPrecision(18, 2);
        builder.HasOne<ServiceOrderEntity>().WithMany(x => x.EstimateApprovals)
            .HasForeignKey(x => x.ServiceOrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
