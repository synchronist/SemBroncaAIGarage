using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;
using SemBroncaAI.Garage.Domain.Interfaces;
namespace SemBroncaAI.Garage.Application.Features.Vehicles.UpdateVehicle;
public sealed class UpdateVehicleHandler(ICustomerRepository customerRepository, IVehicleRepository vehicleRepository, IUnitOfWork unitOfWork)
{
    public async Task<UpdateVehicleResponse> HandleAsync(Guid id, UpdateVehicleCommand command, CancellationToken cancellationToken = default)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(id, command.GarageId, cancellationToken) ?? throw new InvalidOperationException("Veículo não encontrado.");
        var customer = await customerRepository.GetByIdAsync(command.CustomerId, command.GarageId, cancellationToken) ?? throw new InvalidOperationException("Cliente não encontrado.");
        var plate = VehicleEntity.NormalizePlate(command.Plate);
        if (await vehicleRepository.ExistsByPlateAsync(command.GarageId, plate, id, cancellationToken)) throw new InvalidOperationException("Já existe um veículo com essa placa nesta oficina.");
        vehicle.Update(customer.Id, plate, command.Brand, command.Model, command.Version, command.Year, command.Color, command.Fuel, command.Mileage);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new UpdateVehicleResponse(vehicle.Id, vehicle.GarageId, vehicle.CustomerId, vehicle.Plate, vehicle.Brand, vehicle.Model, vehicle.Version, vehicle.Year, vehicle.Color, vehicle.Fuel, vehicle.Mileage, vehicle.Active, vehicle.CreatedAt);
    }
}
