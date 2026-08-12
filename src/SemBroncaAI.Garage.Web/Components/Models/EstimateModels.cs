namespace SemBroncaAI.Garage.Web.Models;

public sealed record EstimateListModel(int Page, int PageSize, int TotalItems, int TotalPages,
    IReadOnlyCollection<EstimateListItemModel> Items, EstimateIndicatorsModel Indicators);

public sealed record EstimateIndicatorsModel(int Pending, int Approved, int Rejected, decimal PendingValue);

public sealed record EstimateListItemModel(Guid ServiceOrderId, int ServiceOrderNumber,
    string ServiceOrderStatus, DateTimeOffset ServiceOrderCreatedAt, string CustomerName,
    string CustomerPhone, string Vehicle, string Plate, decimal Total, string CommercialStatus,
    DateTimeOffset? SentAt, DateTimeOffset? RespondedAt, string? CustomerComment, string? ApprovalToken);

public sealed record WhatsAppShareModel(string CustomerName, string Phone, string Message,
    string ApprovalLink, string? WhatsAppPhone);
