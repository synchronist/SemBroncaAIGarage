using Microsoft.EntityFrameworkCore;
using GarageEntity = global::SemBroncaAI.Garage.Domain.Entities.Garage;

namespace SemBroncaAI.Garage.Infrastructure.Persistence;

public sealed class GarageDbContext : DbContext
{
    public GarageDbContext(DbContextOptions<GarageDbContext> options)
        : base(options)
    {
    }

    public DbSet<GarageEntity> Garages => Set<GarageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GarageDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}