using SemBroncaAI.Garage.Domain.Entities.Vehicle;

namespace SemBroncaAI.Garage.Domain.Interfaces;

public interface IVehicleRepository
{
    Task AddAsync(
        VehicleEntity vehicle,
        CancellationToken cancellationToken);

    Task<VehicleEntity?> GetByIdAsync(
        Guid id,
        Guid garageId,
        CancellationToken cancellationToken);

    Task<bool> ExistsByPlateAsync(Guid garageId, string plate, Guid? excludingVehicleId = null, CancellationToken cancellationToken = default);
}
