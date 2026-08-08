using SemBroncaAI.Garage.Domain.Entities.Customer;

namespace SemBroncaAI.Garage.Application.Abstractions.Persistence;

public interface ICustomerRepository
{
    Task AddAsync(
        CustomerEntity customer,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByDocumentAsync(
        Guid garageId,
        string document,
        Guid? excludingCustomerId = null,
        CancellationToken cancellationToken = default);

    Task<CustomerEntity?> GetByIdAsync(
        Guid id,
        Guid garageId,
        CancellationToken cancellationToken = default);
}
