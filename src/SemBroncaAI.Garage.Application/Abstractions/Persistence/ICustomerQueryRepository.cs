using SemBroncaAI.Garage.Application.Features.Customers.GetCustomerById;
using SemBroncaAI.Garage.Application.Features.Customers.ListCustomers;

namespace SemBroncaAI.Garage.Application.Abstractions.Persistence;

public interface ICustomerQueryRepository
{
    Task<ListCustomersResponse> ListAsync(ListCustomersQuery query, CancellationToken cancellationToken = default);
    Task<GetCustomerByIdResponse?> GetByIdAsync(Guid id, Guid garageId, CancellationToken cancellationToken = default);
}
