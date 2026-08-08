using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.ListServiceOrders;

public sealed record ListServiceOrdersItem(
    Guid Id,
    int Number,
    ServiceOrderStatus Status,
    DateTimeOffset CreatedAt,
    string CustomerComplaint,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    Guid VehicleId,
    string Plate,
    string Brand,
    string Model,
    string Version,
    int Year);