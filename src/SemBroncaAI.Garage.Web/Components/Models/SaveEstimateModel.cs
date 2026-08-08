namespace SemBroncaAI.Garage.Web.Models;

public sealed record SaveEstimateRequest(
    IReadOnlyCollection<SaveEstimateItemRequest> Items);

public sealed record SaveEstimateItemRequest(
    string Description,
    int Type,
    decimal Quantity,
    decimal UnitPrice);

public sealed record SaveEstimateResponse(
    Guid Id,
    decimal ServicesSubtotal,
    decimal PartsSubtotal,
    decimal Total,
    int ItemCount);
