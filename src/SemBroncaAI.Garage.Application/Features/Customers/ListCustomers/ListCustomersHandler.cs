using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Domain.Common;
using SemBroncaAI.Garage.Application.Common;

namespace SemBroncaAI.Garage.Application.Features.Customers.ListCustomers;

public sealed class ListCustomersHandler
{
    private readonly ICustomerQueryRepository _repository;
    public ListCustomersHandler(ICustomerQueryRepository repository) => _repository = repository;

    public Task<ListCustomersResponse> HandleAsync(ListCustomersQuery query, CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmpty(query.GarageId, nameof(query.GarageId));
        PaginationRules.Validate(query.Page, query.PageSize);
        return _repository.ListAsync(query, cancellationToken);
    }
}
