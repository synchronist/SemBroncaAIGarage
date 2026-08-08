namespace SemBroncaAI.Garage.Web.Models;

public sealed record ServiceOrderHistoryItem(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    string Description,
    Guid? ActorId,
    DateTimeOffset CreatedAt);

public sealed record ServiceOrderCustomerModel(
    Guid Id,
    string Name,
    string Document,
    string Phone,
    string Email);

public sealed record ServiceOrderVehicleModel(
    Guid Id,
    string Plate,
    string Brand,
    string Model,
    string Version,
    int Year,
    string Color,
    string Fuel,
    int Mileage);

public sealed record ServiceOrderDetailsModel(
    Guid Id,
    Guid GarageId,
    int Number,
    string Status,
    string CustomerComplaint,
    DateTimeOffset CreatedAt,
    ServiceOrderCustomerModel Customer,
    ServiceOrderVehicleModel Vehicle,
    IReadOnlyCollection<ServiceOrderHistoryItem> History,
    ServiceOrderDiagnosisModel? Diagnosis);

public sealed record ServiceOrderDiagnosisModel(
    Guid Id,
    string Description,
    string InternalNotes,
    Guid? ActorId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);