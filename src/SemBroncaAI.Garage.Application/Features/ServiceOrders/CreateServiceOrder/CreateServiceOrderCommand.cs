namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.CreateServiceOrder;

public sealed record CreateServiceOrderCommand(
    Guid GarageId,
    Guid VehicleId,
    string CustomerComplaint,
    int Mileage);
