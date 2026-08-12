using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;

public sealed record ApprovalSummaryResponse(EstimateApprovalStatus Status, DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt, DateTimeOffset? RespondedAt, string? CustomerName, string? CustomerComment,
    string? Token);

public sealed record PublicApprovalResponse(string GarageName, string? GaragePhone, string? GarageEmail,
    string? LogoUrl, string PrimaryColor, int ServiceOrderNumber, string CustomerName,
    string Vehicle, string Plate, string? Diagnosis, string CustomerComplaint, int? Mileage,
    EstimateApprovalStatus Status, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt,
    DateTimeOffset? RespondedAt, string? CustomerComment, decimal ServicesSubtotal,
    decimal PartsSubtotal, decimal Total, IReadOnlyCollection<PublicApprovalItemResponse> Items);

public sealed record PublicApprovalItemResponse(string Description, EstimateItemType Type,
    decimal Quantity, decimal UnitPrice, decimal Total);
public sealed record ApprovalDecisionRequest(string? CustomerName, string? Comment);
