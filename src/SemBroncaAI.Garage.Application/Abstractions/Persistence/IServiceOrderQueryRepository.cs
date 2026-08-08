using SemBroncaAI.Garage.Application.Features.ServiceOrders.ListServiceOrders;

namespace SemBroncaAI.Garage.Application.Abstractions.Persistence;

public interface IServiceOrderQueryRepository
{
    Task<ListServiceOrdersResponse> ListAsync(
        ListServiceOrdersQuery query,
        CancellationToken cancellationToken = default);
}