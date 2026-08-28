using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Interfaces;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Infrastructure.Repositories;

public sealed class ServiceOrderRepository : IServiceOrderRepository
{
    private readonly GarageDbContext _context;

    public ServiceOrderRepository(GarageDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        ServiceOrderEntity serviceOrder,
        CancellationToken cancellationToken = default)
    {
        await _context.ServiceOrders.AddAsync(
            serviceOrder,
            cancellationToken);
    }

    public async Task<ServiceOrderEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ServiceOrders
            .AsSplitQuery()
            .Include(x => x.Vehicle)
                .ThenInclude(x => x.Customer)
            .Include(x => x.History)
            .Include(x => x.Diagnosis)
            .Include(x => x.Estimate!)
                .ThenInclude(x => x.Items)
            .Include(x => x.EstimateApprovals)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<ServiceOrderEntity?> GetByNumberAsync(
        Guid garageId,
        int number,
        CancellationToken cancellationToken = default)
    {
        return await _context.ServiceOrders
            .AsSplitQuery()
            .Include(x => x.Vehicle)
                .ThenInclude(x => x.Customer)
            .Include(x => x.History)
            .Include(x => x.Diagnosis)
            .Include(x => x.Estimate!)
                .ThenInclude(x => x.Items)
            .Include(x => x.EstimateApprovals)
            .FirstOrDefaultAsync(
                x => x.GarageId == garageId &&
                     x.Number == number,
                cancellationToken);
    }

    public async Task<ServiceOrderEntity?> GetByIdAsync(
        Guid id,
        Guid garageId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ServiceOrders
            .AsSplitQuery()
            .Include(x => x.Vehicle).ThenInclude(x => x.Customer)
            .Include(x => x.History)
            .Include(x => x.Diagnosis)
            .Include(x => x.Estimate!).ThenInclude(x => x.Items)
            .Include(x => x.EstimateApprovals)
            .FirstOrDefaultAsync(x => x.Id == id && x.GarageId == garageId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ServiceOrderEntity>> ListVehicleHistoryAsync(
        Guid garageId,
        Guid vehicleId,
        Guid excludeServiceOrderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ServiceOrders
            .AsNoTracking()
            .Where(x => x.GarageId == garageId &&
                        x.VehicleId == vehicleId &&
                        x.Id != excludeServiceOrderId)
            .Include(x => x.Diagnosis)
            .Include(x => x.Estimate!)
                .ThenInclude(x => x.Items)
            .OrderByDescending(x => x.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ServiceOrderTechnicalHistoryPage> ListVehicleTechnicalHistoryAsync(
        Guid garageId, Guid vehicleId, Guid excludeServiceOrderId,
        int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceOrders.AsNoTracking()
            .Where(x => x.GarageId == garageId && x.VehicleId == vehicleId &&
                        x.Id != excludeServiceOrderId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Number)
            .Skip(offset).Take(pageSize)
            .Include(x => x.Diagnosis)
            .Include(x => x.Estimate!).ThenInclude(x => x.Items)
            .ToArrayAsync(cancellationToken);
        return new(totalCount, items);
    }

    public async Task<ServiceOrderEntity?> GetByApprovalTokenHashAsync(string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await _context.ServiceOrders
            .AsSplitQuery()
            .Include(x => x.Garage)
            .Include(x => x.Vehicle).ThenInclude(x => x.Customer)
            .Include(x => x.Diagnosis)
            .Include(x => x.Estimate!).ThenInclude(x => x.Items)
            .Include(x => x.EstimateApprovals)
            .FirstOrDefaultAsync(x => x.EstimateApprovals.Any(a => a.TokenHash == tokenHash), cancellationToken);
    }

    public void RemoveEstimateItems(
        IEnumerable<ServiceOrderEstimateItemEntity> items)
    {
        _context.ServiceOrderEstimateItems.RemoveRange(items);
    }
}
