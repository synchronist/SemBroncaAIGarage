using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemBroncaAI.Garage.Domain.Entities;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Configurations;

public sealed class TeamInvitationConfiguration : IEntityTypeConfiguration<TeamInvitationEntity>
{
    public void Configure(EntityTypeBuilder<TeamInvitationEntity> builder)
    {
        builder.ToTable("TeamInvitations"); builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UsedAt).IsConcurrencyToken();
        builder.HasIndex(x => x.TokenHash).IsUnique(); builder.HasIndex(x => new { x.GarageId, x.UserId });
        builder.HasOne<Identity.ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Identity.ApplicationUser>().WithMany().HasForeignKey(x => x.InvitedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
