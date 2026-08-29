using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using System.Text.Json.Serialization;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.Approval;

public sealed record ApprovalSummaryResponse(EstimateApprovalStatus Status, DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt, DateTimeOffset? RespondedAt, string? CustomerName, string? CustomerComment,
    string? Token)
{
    public decimal? ApprovedTotal { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CustomerDocumentMasked { get; init; }
    public IReadOnlyCollection<Guid> ApprovedItemIds { get; init; } = [];
}

public sealed record PublicApprovalResponse(string GarageName, string? GaragePhone, string? GarageEmail,
    string? LogoUrl, string PrimaryColor, int ServiceOrderNumber, string CustomerName,
    string Vehicle, string Plate, string? Diagnosis, string CustomerComplaint, int? Mileage,
    EstimateApprovalStatus Status, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt,
    DateTimeOffset? RespondedAt, string? CustomerComment, decimal ServicesSubtotal,
    decimal PartsSubtotal, decimal Total, IReadOnlyCollection<PublicApprovalItemResponse> Items)
{
    public decimal? ApprovedTotal { get; init; }
    public string? ApprovalType { get; init; }
    public string? DeclaredApproverName { get; init; }
    public string? DeclaredApproverDocumentMasked { get; init; }
}

public sealed record PublicApprovalItemResponse(Guid Id, string Description, EstimateItemType Type,
    decimal Quantity, decimal UnitPrice, decimal Total, bool? Approved = null);
public sealed record ApprovalDecisionRequest(string CustomerName, string CustomerDocument,
    string CustomerPhone, IReadOnlyCollection<Guid> ApprovedItemIds, string? Comment);

public interface IApprovalValidityProvider
{
    int DefaultValidityDays { get; }
}
