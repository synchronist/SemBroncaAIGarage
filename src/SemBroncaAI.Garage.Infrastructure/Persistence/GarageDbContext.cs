using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using SemBroncaAI.Garage.Infrastructure.Identity;
using SemBroncaAI.Garage.Domain.Entities.Customer;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;
using SemBroncaAI.Garage.Domain.Interfaces;
using SemBroncaAI.Garage.Domain.Entities;
using Npgsql;
using SemBroncaAI.Garage.Domain.Entities.SiteManagement;

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
    public DbSet<ServiceOrderNumberSequence> ServiceOrderNumberSequences => Set<ServiceOrderNumberSequence>();
    public DbSet<TeamInvitationEntity> TeamInvitations => Set<TeamInvitationEntity>();
    public DbSet<GarageSubscriptionEntity> GarageSubscriptions => Set<GarageSubscriptionEntity>();
    public DbSet<AuditEntryEntity> AuditEntries => Set<AuditEntryEntity>();
    public DbSet<ProcessedBillingEvent> ProcessedBillingEvents => Set<ProcessedBillingEvent>();
    public DbSet<ManagedSiteEntity> ManagedSites => Set<ManagedSiteEntity>();
    public DbSet<ManagedSiteMailboxEntity> ManagedSiteMailboxes => Set<ManagedSiteMailboxEntity>();
    public DbSet<ManagedSiteCostEntity> ManagedSiteCosts => Set<ManagedSiteCostEntity>();
    public DbSet<ManagedSiteSupportEntity> ManagedSiteSupportEntries => Set<ManagedSiteSupportEntity>();
    public DbSet<ManagedSiteHistoryEntity> ManagedSiteHistory => Set<ManagedSiteHistoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(GarageDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        IncrementServiceOrderVersions();

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: DatabaseConstraintNames.UniqueServiceOrderNumberPerGarage
            })
        {
            throw new InvalidOperationException(
                "Não foi possível reservar o número da ordem de serviço. Tente novamente.",
                exception);
        }
    }

    private void IncrementServiceOrderVersions()
    {
        var changedStates = new[] { EntityState.Added, EntityState.Modified, EntityState.Deleted };
        var aggregateIds = new HashSet<Guid>(
            ChangeTracker.Entries<ServiceOrderEntity>()
                .Where(entry => entry.State == EntityState.Modified)
                .Select(entry => entry.Entity.Id));

        aggregateIds.UnionWith(ChangeTracker.Entries<ServiceOrderHistoryEntity>()
            .Where(entry => changedStates.Contains(entry.State)).Select(entry => entry.Entity.ServiceOrderId));
        aggregateIds.UnionWith(ChangeTracker.Entries<ServiceOrderDiagnosisEntity>()
            .Where(entry => changedStates.Contains(entry.State)).Select(entry => entry.Entity.ServiceOrderId));
        aggregateIds.UnionWith(ChangeTracker.Entries<ServiceOrderEstimateEntity>()
            .Where(entry => changedStates.Contains(entry.State)).Select(entry => entry.Entity.ServiceOrderId));
        aggregateIds.UnionWith(ChangeTracker.Entries<ServiceOrderEstimateApprovalEntity>()
            .Where(entry => changedStates.Contains(entry.State)).Select(entry => entry.Entity.ServiceOrderId));

        var changedEstimateIds = ChangeTracker.Entries<ServiceOrderEstimateItemEntity>()
            .Where(entry => changedStates.Contains(entry.State))
            .Select(entry => entry.Entity.EstimateId)
            .ToHashSet();
        aggregateIds.UnionWith(ChangeTracker.Entries<ServiceOrderEstimateEntity>()
            .Where(entry => changedEstimateIds.Contains(entry.Entity.Id))
            .Select(entry => entry.Entity.ServiceOrderId));

        foreach (var entry in ChangeTracker.Entries<ServiceOrderEntity>()
                     .Where(entry => entry.State != EntityState.Added && aggregateIds.Contains(entry.Entity.Id)))
        {
            entry.State = EntityState.Modified;
            var version = entry.Property(order => order.Version);
            version.CurrentValue = version.OriginalValue + 1;
        }
    }
}
