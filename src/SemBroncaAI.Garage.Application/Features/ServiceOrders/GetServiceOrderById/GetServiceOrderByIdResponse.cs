using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.GetServiceOrderById;

public sealed record ServiceOrderHistoryResponse(
    Guid Id,
    ServiceOrderStatus? FromStatus,
    ServiceOrderStatus ToStatus,
    string Description,
    Guid? ActorId,
    DateTimeOffset CreatedAt);

public sealed record ServiceOrderCustomerResponse(
    Guid Id,
    string Name,
    string Document,
    string Phone,
    string Email);

public sealed record ServiceOrderVehicleResponse(
    Guid Id,
    string Plate,
    string Brand,
    string Model,
    string Version,
    int Year,
    string Color,
    string Fuel,
    int Mileage);

public sealed record GetServiceOrderByIdResponse(
    Guid Id,
    Guid GarageId,
    int Number,
    ServiceOrderStatus Status,
    string CustomerComplaint,
    DateTimeOffset CreatedAt,
    ServiceOrderCustomerResponse Customer,
    ServiceOrderVehicleResponse Vehicle,
    ServiceOrderDiagnosisResponse? Diagnosis,
    ServiceOrderEstimateResponse? Estimate,
    IReadOnlyCollection<ServiceOrderHistoryResponse> History);
public sealed record ServiceOrderDiagnosisResponse(
    Guid Id,
    string Description,
    string InternalNotes,
    Guid? ActorId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ServiceOrderEstimateResponse(
    Guid Id,
    decimal ServicesSubtotal,
    decimal PartsSubtotal,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<ServiceOrderEstimateItemResponse> Items);

public sealed record ServiceOrderEstimateItemResponse(
    Guid Id,
    string Description,
    EstimateItemType Type,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total);
