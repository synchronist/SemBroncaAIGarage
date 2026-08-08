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
        Guid garageId,
        CancellationToken cancellationToken)
    {
        return await context.Vehicles
            .FirstOrDefaultAsync(x => x.Id == id && x.GarageId == garageId, cancellationToken);
    }

    public Task<bool> ExistsByPlateAsync(Guid garageId, string plate, Guid? excludingVehicleId = null, CancellationToken cancellationToken = default) =>
        context.Vehicles.AnyAsync(x => x.GarageId == garageId && x.Plate == plate && (!excludingVehicleId.HasValue || x.Id != excludingVehicleId.Value), cancellationToken);
}
