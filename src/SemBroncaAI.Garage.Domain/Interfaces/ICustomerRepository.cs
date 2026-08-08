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
        CancellationToken cancellationToken = default);

    Task<CustomerEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerEntity>> GetAllAsync(
        Guid garageId,
        CancellationToken cancellationToken = default);
}