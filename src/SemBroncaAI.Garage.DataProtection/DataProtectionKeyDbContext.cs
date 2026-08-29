using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SemBroncaAI.Garage.DataProtection;

public sealed class DataProtectionKeyDbContext(DbContextOptions<DataProtectionKeyDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataProtectionKey>(entity =>
        {
            entity.ToTable("DataProtectionKeys");
            entity.HasKey(key => key.Id);
            entity.Property(key => key.Id).UseIdentityByDefaultColumn();
            entity.Property(key => key.FriendlyName).HasColumnType("text");
            entity.Property(key => key.Xml).HasColumnType("text");
        });
    }
}
