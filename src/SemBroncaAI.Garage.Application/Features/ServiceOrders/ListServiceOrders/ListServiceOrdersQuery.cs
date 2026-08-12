using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.ListServiceOrders;

public enum ServiceOrderArchiveFilter
{
    Active,
    Archived,
    All
}

public sealed record ListServiceOrdersQuery(
    Guid GarageId,
    string? Search = null,
    ServiceOrderStatus? Status = null,
    ServiceOrderArchiveFilter Archive = ServiceOrderArchiveFilter.Active,
    int Page = 1,
    int PageSize = 20);
