using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Domain.Common;

namespace SemBroncaAI.Garage.Application.Features.Customers.ListCustomers;

public sealed class ListCustomersHandler
{
    private readonly ICustomerQueryRepository _repository;
    public ListCustomersHandler(ICustomerQueryRepository repository) => _repository = repository;

    public Task<ListCustomersResponse> HandleAsync(ListCustomersQuery query, CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmpty(query.GarageId, nameof(query.GarageId));
        return _repository.ListAsync(query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 100)
        }, cancellationToken);
    }
}
