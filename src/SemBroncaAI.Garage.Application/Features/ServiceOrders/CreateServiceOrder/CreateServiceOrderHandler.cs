using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.CreateServiceOrder;

public sealed class CreateServiceOrderHandler
{
    private readonly IGarageRepository _garageRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IServiceOrderRepository _serviceOrderRepository;
    private readonly IServiceOrderNumberGenerator _numberGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateServiceOrderHandler(
        IGarageRepository garageRepository,
        IVehicleRepository vehicleRepository,
        IServiceOrderRepository serviceOrderRepository,
        IServiceOrderNumberGenerator numberGenerator,
        IUnitOfWork unitOfWork)
    {
        _garageRepository = garageRepository;
        _vehicleRepository = vehicleRepository;
        _serviceOrderRepository = serviceOrderRepository;
        _numberGenerator = numberGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateServiceOrderResponse> HandleAsync(
        CreateServiceOrderCommand command,
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

        var vehicle = await _vehicleRepository.GetByIdAsync(
            command.VehicleId,
            command.GarageId,
            cancellationToken);

        if (vehicle is null)
        {
            throw new InvalidOperationException(
                "Veículo não encontrado.");
        }

        if (vehicle.GarageId != command.GarageId)
        {
            throw new InvalidOperationException(
                "O veículo não pertence à oficina informada.");
        }

        vehicle.UpdateMileage(command.Mileage);

        var number = await _numberGenerator.GetNextAsync(
            command.GarageId,
            cancellationToken);

        var serviceOrder = new ServiceOrderEntity(
            command.GarageId,
            command.VehicleId,
            number,
            command.CustomerComplaint,
            command.Mileage);

        await _serviceOrderRepository.AddAsync(
            serviceOrder,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateServiceOrderResponse(
            serviceOrder.Id,
            serviceOrder.Number);
    }
}
