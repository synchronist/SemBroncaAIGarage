namespace SemBroncaAI.Garage.Application.Features.Estimates.ListEstimates;

public enum EstimateCommercialStatus
{
    NotSent,
    Pending,
    Approved,
    PartiallyApproved,
    Rejected,
    Expired
}

public sealed record ListEstimatesQuery(
    Guid GarageId,
    string? Search,
    EstimateCommercialStatus? Status,
    int Page = 1,
    int PageSize = 10);

public sealed record ListEstimatesItem(
    Guid ServiceOrderId,
    int ServiceOrderNumber,
    string ServiceOrderStatus,
    DateTimeOffset ServiceOrderCreatedAt,
    string CustomerName,
    string CustomerPhone,
    string Vehicle,
    string Plate,
    decimal Total,
    EstimateCommercialStatus CommercialStatus,
    DateTimeOffset? SentAt,
    DateTimeOffset? RespondedAt,
    string? CustomerComment,
    string? ApprovalToken);

public sealed record EstimateIndicators(
    int Pending,
    int Approved,
    int Rejected,
    decimal PendingValue);

public sealed record ListEstimatesResponse(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    IReadOnlyCollection<ListEstimatesItem> Items,
    EstimateIndicators Indicators);
