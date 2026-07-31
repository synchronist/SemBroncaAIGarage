using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Domain.Interfaces;
using SemBroncaAI.Garage.Domain.Entities;

namespace SemBroncaAI.Garage.Infrastructure.Persistence;

public sealed class GarageDbContext : DbContext, IUnitOfWork
{
    public GarageDbContext(DbContextOptions<GarageDbContext> options)
        : base(options)
    {
    }

    public DbSet<GarageEntity> Garages => Set<GarageEntity>();
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GarageDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}