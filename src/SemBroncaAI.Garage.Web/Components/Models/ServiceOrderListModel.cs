namespace SemBroncaAI.Garage.Web.Models;

public sealed record ServiceOrderListItemModel(
    Guid Id,
    int Number,
    string Status,
    DateTimeOffset CreatedAt,
    string CustomerComplaint,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    Guid VehicleId,
    string Plate,
    string Brand,
    string Model,
    string Version,
    int Year);

public sealed record ServiceOrderListModel(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    IReadOnlyCollection<ServiceOrderListItemModel> Items);