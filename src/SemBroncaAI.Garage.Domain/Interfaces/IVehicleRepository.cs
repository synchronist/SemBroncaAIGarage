using SemBroncaAI.Garage.Domain.Entities.Vehicle;

namespace SemBroncaAI.Garage.Domain.Interfaces;

public interface IVehicleRepository
{
    Task AddAsync(
        VehicleEntity vehicle,
        CancellationToken cancellationToken);

    Task<VehicleEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IEnumerable<VehicleEntity>> GetAllAsync(
        CancellationToken cancellationToken);
}