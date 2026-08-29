namespace SemBroncaAI.Garage.Web.Models;

public sealed record PublicApprovalModel(string GarageName, string? GaragePhone, string? GarageEmail,
    string? LogoUrl, string PrimaryColor, int ServiceOrderNumber, string CustomerName,
    string Vehicle, string Plate, string? Diagnosis, string CustomerComplaint, int? Mileage,
    string Status, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, DateTimeOffset? RespondedAt,
    string? CustomerComment, decimal ServicesSubtotal, decimal PartsSubtotal, decimal Total,
    IReadOnlyCollection<PublicApprovalItemModel> Items, decimal? ApprovedTotal,
    string? ApprovalType, string? DeclaredApproverName, string? DeclaredApproverDocumentMasked);
public sealed record PublicApprovalItemModel(Guid Id, string Description, string Type, decimal Quantity,
    decimal UnitPrice, decimal Total, bool? Approved);
public sealed record PublicApprovalDecision(string CustomerName, string CustomerDocument,
    string CustomerPhone, IReadOnlyCollection<Guid> ApprovedItemIds, string? Comment);
