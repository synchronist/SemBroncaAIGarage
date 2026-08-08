using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Domain.Entities.Customer;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Infrastructure.Persistence;

public sealed class GarageDbContext : DbContext, IUnitOfWork
{
    public GarageDbContext(DbContextOptions<GarageDbContext> options)
        : base(options)
    {
    }

    public DbSet<GarageEntity> Garages => Set<GarageEntity>();
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<VehicleEntity> Vehicles => Set<VehicleEntity>();
    public DbSet<ServiceOrderEntity> ServiceOrders => Set<ServiceOrderEntity>();
    public DbSet<ServiceOrderHistoryEntity> ServiceOrderHistories =>
        Set<ServiceOrderHistoryEntity>();
    public DbSet<ServiceOrderDiagnosisEntity> ServiceOrderDiagnoses =>
    Set<ServiceOrderDiagnosisEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(GarageDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}