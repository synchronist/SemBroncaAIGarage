namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.ListServiceOrders;

public sealed record ListServiceOrdersResponse(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    IReadOnlyCollection<ListServiceOrdersItem> Items);