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
    string? Document,
    string? Phone,
    string? Email);

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
    int? Mileage,
    DateTimeOffset CreatedAt,
    ServiceOrderCustomerModel Customer,
    ServiceOrderVehicleModel Vehicle,
    IReadOnlyCollection<ServiceOrderHistoryItem> History,
    ServiceOrderDiagnosisModel? Diagnosis,
    ServiceOrderEstimateModel? Estimate,
    ServiceOrderApprovalModel? Approval = null,
    IReadOnlyCollection<ServiceOrderApprovalHistoryModel>? ApprovalHistory = null,
    DateTimeOffset? ArchivedAt = null,
    IReadOnlyCollection<ServiceOrderTechnicalHistoryModel>? TechnicalHistory = null,
    DateTimeOffset? DigitalApprovalWaivedAt = null);

public sealed record ServiceOrderTechnicalHistoryModel(
    Guid Id,
    int Number,
    string Status,
    string CustomerComplaint,
    int? Mileage,
    DateTimeOffset CreatedAt,
    string? Diagnosis,
    string? InternalNotes,
    IReadOnlyCollection<string> WorkItems);

public sealed record ServiceOrderTechnicalHistoryPageModel(
    int Offset, int PageSize, int TotalCount,
    IReadOnlyCollection<ServiceOrderTechnicalHistoryModel> Items);

public sealed record ServiceOrderApprovalModel(string Status, DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt, DateTimeOffset? RespondedAt, string? CustomerName,
    string? CustomerComment, string? Token)
{
    public decimal? ApprovedTotal { get; init; }
    public string? CustomerDocumentMasked { get; init; }
    public IReadOnlyCollection<Guid> ApprovedItemIds { get; init; } = [];
}

public sealed record ServiceOrderApprovalHistoryModel(string Status, DateTimeOffset CreatedAt,
      DateTimeOffset ExpiresAt, DateTimeOffset? RespondedAt, DateTimeOffset? InvalidatedAt,
      string? CustomerName, string? CustomerComment)
{
    public decimal? ApprovedTotal { get; init; }
    public string? CustomerDocumentMasked { get; init; }
    public IReadOnlyCollection<Guid> ApprovedItemIds { get; init; } = [];
}

public sealed record ServiceOrderDiagnosisModel(
    Guid Id,
    string Description,
    string InternalNotes,
    Guid? ActorId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ServiceOrderEstimateModel(
    Guid Id,
    decimal ServicesSubtotal,
    decimal PartsSubtotal,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<ServiceOrderEstimateItemModel> Items);

public sealed record ServiceOrderEstimateItemModel(
    Guid Id,
    string Description,
    string Type,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total,
    string AuthorizationStatus = "Pending");
