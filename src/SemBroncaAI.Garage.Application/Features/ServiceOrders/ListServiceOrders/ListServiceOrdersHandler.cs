using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Common;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.ListServiceOrders;

public sealed class ListServiceOrdersHandler(IServiceOrderQueryRepository repository)
{
    public Task<ListServiceOrdersResponse> HandleAsync(
        ListServiceOrdersQuery query, CancellationToken cancellationToken = default)
    {
        if (query.GarageId == Guid.Empty)
            throw new ArgumentException("A oficina é obrigatória.", nameof(query.GarageId));
        PaginationRules.Validate(query.Page, query.PageSize);
        return repository.ListAsync(query with { Search = query.Search?.Trim() }, cancellationToken);
    }
}
