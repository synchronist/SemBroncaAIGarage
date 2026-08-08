using SemBroncaAI.Garage.Application.Abstractions.Persistence;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.ListServiceOrders;

public sealed class ListServiceOrdersHandler
{
    private readonly IServiceOrderQueryRepository _repository;

    public ListServiceOrdersHandler(
        IServiceOrderQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<ListServiceOrdersResponse> HandleAsync(
        ListServiceOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.GarageId == Guid.Empty)
        {
            throw new ArgumentException(
                "A oficina é obrigatória.",
                nameof(query.GarageId));
        }

        var page =
            query.Page <= 0
                ? 1
                : query.Page;

        var pageSize =
            query.PageSize switch
            {
                <= 0 => 20,
                > 100 => 100,
                _ => query.PageSize
            };

        var normalizedQuery =
            query with
            {
                Page = page,
                PageSize = pageSize,
                Search = query.Search?.Trim()
            };

        return await _repository.ListAsync(
            normalizedQuery,
            cancellationToken);
    }
}