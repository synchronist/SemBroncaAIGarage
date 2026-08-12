using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SemBroncaAI.Garage.Infrastructure.Identity;
using SemBroncaAI.Garage.Domain.Entities.Customer;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Infrastructure.Persistence;

public sealed class GarageDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IUnitOfWork
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
    public DbSet<ServiceOrderEstimateEntity> ServiceOrderEstimates =>
        Set<ServiceOrderEstimateEntity>();
    public DbSet<ServiceOrderEstimateItemEntity> ServiceOrderEstimateItems =>
        Set<ServiceOrderEstimateItemEntity>();
    public DbSet<ServiceOrderEstimateApprovalEntity> ServiceOrderEstimateApprovals => Set<ServiceOrderEstimateApprovalEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(GarageDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
