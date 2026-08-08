using SemBroncaAI.Garage.Application.Abstractions.Persistence;

namespace SemBroncaAI.Garage.Application.Features.Customers.GetCustomerById;

public sealed class GetCustomerByIdHandler
{
    private readonly ICustomerQueryRepository _repository;
    public GetCustomerByIdHandler(ICustomerQueryRepository repository) => _repository = repository;
    public Task<GetCustomerByIdResponse?> HandleAsync(Guid id, Guid garageId, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, garageId, cancellationToken);
}
