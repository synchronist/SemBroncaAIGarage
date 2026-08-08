using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.ListServiceOrders;

public sealed record ListServiceOrdersQuery(
    Guid GarageId,
    string? Search = null,
    ServiceOrderStatus? Status = null,
    int Page = 1,
    int PageSize = 20);