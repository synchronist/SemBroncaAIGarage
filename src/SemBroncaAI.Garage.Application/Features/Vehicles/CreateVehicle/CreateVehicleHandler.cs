using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.Vehicles.CreateVehicle;

public sealed class CreateVehicleHandler
{
    private readonly IGarageRepository _garageRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateVehicleHandler(
        IGarageRepository garageRepository,
        ICustomerRepository customerRepository,
        IVehicleRepository vehicleRepository,
        IUnitOfWork unitOfWork)
    {
        _garageRepository = garageRepository;
        _customerRepository = customerRepository;
        _vehicleRepository = vehicleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateVehicleResponse> HandleAsync(
        CreateVehicleCommand command,
        CancellationToken cancellationToken = default)
    {
        var garage = await _garageRepository.GetByIdAsync(
            command.GarageId,
            cancellationToken);

        if (garage is null)
        {
            throw new InvalidOperationException(
                "Oficina não encontrada.");
        }

        var customer = await _customerRepository.GetByIdAsync(
            command.CustomerId,
            cancellationToken);

        if (customer is null)
        {
            throw new InvalidOperationException(
                "Cliente não encontrado.");
        }

        if (customer.GarageId != command.GarageId)
        {
            throw new InvalidOperationException(
                "O cliente não pertence à oficina informada.");
        }

        var vehicle = new VehicleEntity(
            command.GarageId,
            command.CustomerId,
            command.Plate,
            command.Brand,
            command.Model,
            command.Version,
            command.Year,
            command.Color,
            command.Fuel,
            command.Mileage);

        await _vehicleRepository.AddAsync(
            vehicle,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateVehicleResponse(
            vehicle.Id,
            vehicle.GarageId,
            vehicle.CustomerId,
            vehicle.Plate,
            vehicle.Brand,
            vehicle.Model,
            vehicle.Version,
            vehicle.Year,
            vehicle.Color,
            vehicle.Fuel,
            vehicle.Mileage,
            vehicle.Active,
            vehicle.CreatedAt);
    }
}