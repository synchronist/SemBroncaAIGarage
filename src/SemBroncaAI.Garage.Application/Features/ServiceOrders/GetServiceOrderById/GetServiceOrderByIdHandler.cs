using SemBroncaAI.Garage.Domain.Interfaces;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.GetServiceOrderById;

public sealed class GetServiceOrderByIdHandler
{
    private readonly IServiceOrderRepository _serviceOrderRepository;
    ServiceOrderDiagnosisResponse? diagnosisResponse = null;

    public GetServiceOrderByIdHandler(
        IServiceOrderRepository serviceOrderRepository)
    {
        _serviceOrderRepository = serviceOrderRepository;
    }

    public async Task<GetServiceOrderByIdResponse?> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var serviceOrder =
            await _serviceOrderRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (serviceOrder is null)
        {
            return null;
        }

        var history = serviceOrder.History
            .OrderBy(item => item.CreatedAt)
            .Select(item => new ServiceOrderHistoryResponse(
                item.Id,
                item.PreviousStatus,
                item.CurrentStatus,
                item.Description,
                item.ActorId,
                item.CreatedAt))
            .ToArray();

        var customer = serviceOrder.Vehicle.Customer;

        var customerResponse =
            new ServiceOrderCustomerResponse(
                customer.Id,
                customer.Name,
                customer.Document,
                customer.Phone,
                customer.Email);

        var vehicle = serviceOrder.Vehicle;

        var vehicleResponse =
            new ServiceOrderVehicleResponse(
                vehicle.Id,
                vehicle.Plate,
                vehicle.Brand,
                vehicle.Model,
                vehicle.Version,
                vehicle.Year,
                vehicle.Color,
                vehicle.Fuel,
                vehicle.Mileage);



        if (serviceOrder.Diagnosis is not null)
        {
            diagnosisResponse =
                new ServiceOrderDiagnosisResponse(
                    serviceOrder.Diagnosis.Id,
                    serviceOrder.Diagnosis.Description,
                    serviceOrder.Diagnosis.InternalNotes,
                    serviceOrder.Diagnosis.ActorId,
                    serviceOrder.Diagnosis.CreatedAt,
                    serviceOrder.Diagnosis.UpdatedAt);
        }

        ServiceOrderEstimateResponse? estimateResponse = null;

        if (serviceOrder.Estimate is not null)
        {
            var estimate = serviceOrder.Estimate;
            estimateResponse = new ServiceOrderEstimateResponse(
                estimate.Id,
                estimate.ServicesSubtotal,
                estimate.PartsSubtotal,
                estimate.Total,
                estimate.CreatedAt,
                estimate.UpdatedAt,
                estimate.Items.Select(item => new ServiceOrderEstimateItemResponse(
                    item.Id,
                    item.Description,
                    item.Type,
                    item.Quantity,
                    item.UnitPrice,
                    item.Total)).ToArray());
        }

        return new GetServiceOrderByIdResponse(
            serviceOrder.Id,
            serviceOrder.GarageId,
            serviceOrder.Number,
            serviceOrder.Status,
            serviceOrder.CustomerComplaint,
            serviceOrder.CreatedAt,
            customerResponse,
            vehicleResponse,
            diagnosisResponse,
            estimateResponse,
            history);
    }
}
