using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Customers.GetCustomerById;
using SemBroncaAI.Garage.Application.Features.Customers.ListCustomers;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Application.Customers;

public sealed class ListCustomersHandlerTests
{
    [Fact]
    public async Task Should_Preserve_Garage_Boundary_In_List_Query()
    {
        var garageId = Guid.CreateVersion7();
        var repository = new QueryRepositorySpy();
        var handler = new ListCustomersHandler(repository);

        await handler.HandleAsync(new ListCustomersQuery(garageId, "Maria", 1, 20));

        repository.ReceivedQuery.ShouldNotBeNull();
        repository.ReceivedQuery.GarageId.ShouldBe(garageId);
        repository.ReceivedQuery.Search.ShouldBe("Maria");
    }

    private sealed class QueryRepositorySpy : ICustomerQueryRepository
    {
        public ListCustomersQuery? ReceivedQuery { get; private set; }

        public Task<ListCustomersResponse> ListAsync(ListCustomersQuery query, CancellationToken cancellationToken = default)
        {
            ReceivedQuery = query;
            return Task.FromResult(new ListCustomersResponse(query.Page, query.PageSize, 0, 0, []));
        }

        public Task<GetCustomerByIdResponse?> GetByIdAsync(Guid id, Guid garageId, CancellationToken cancellationToken = default) =>
            Task.FromResult<GetCustomerByIdResponse?>(null);
    }
}
