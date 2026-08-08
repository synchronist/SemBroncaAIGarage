using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;
using SemBroncaAI.Garage.Domain.Interfaces;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Infrastructure.Repositories;

public sealed class VehicleRepository(GarageDbContext context)
    : IVehicleRepository
{
    public async Task AddAsync(
        VehicleEntity vehicle,
        CancellationToken cancellationToken)
    {
        await context.Vehicles.AddAsync(vehicle, cancellationToken);
    }

    public async Task<VehicleEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await context.Vehicles
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<VehicleEntity>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await context.Vehicles
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}