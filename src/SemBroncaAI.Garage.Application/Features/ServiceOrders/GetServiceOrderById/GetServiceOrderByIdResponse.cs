using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

using SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;
using System.Text.Json.Serialization;

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
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Document,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Phone,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Email);

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
    int? Mileage,
    DateTimeOffset CreatedAt,
    ServiceOrderCustomerResponse Customer,
    ServiceOrderVehicleResponse Vehicle,
    ServiceOrderDiagnosisResponse? Diagnosis,
    ServiceOrderEstimateResponse? Estimate,
    ApprovalSummaryResponse? Approval,
    IReadOnlyCollection<ApprovalHistoryResponse> ApprovalHistory,
    IReadOnlyCollection<ServiceOrderHistoryResponse> History,
    IReadOnlyCollection<ServiceOrderTechnicalHistoryResponse> TechnicalHistory,
    DateTimeOffset? ArchivedAt);

public sealed record ServiceOrderTechnicalHistoryResponse(
    Guid Id,
    int Number,
    ServiceOrderStatus Status,
    string CustomerComplaint,
    int? Mileage,
    DateTimeOffset CreatedAt,
    string? Diagnosis,
    string? InternalNotes,
    IReadOnlyCollection<string> WorkItems);

public sealed record ApprovalHistoryResponse(
    EstimateApprovalStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RespondedAt,
    DateTimeOffset? InvalidatedAt,
    string? CustomerName,
    string? CustomerComment);
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
