using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Customers.CreateCustomer;
using SemBroncaAI.Garage.Domain.Entities.Customer;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Interfaces;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Application.Customers;

public sealed class CreateCustomerDocumentValidationTests
{
    [Fact]
    public async Task Alphanumeric_Document_Should_Not_Be_Persisted()
    {
        var garage = new GarageEntity("Oficina", "123", "1199", "oficina@test.local");
        var customers = new CustomerRepository();
        var unitOfWork = new UnitOfWork();
        var handler = new CreateCustomerHandler(customers, new GarageRepository(garage), unitOfWork);

        await Should.ThrowAsync<ArgumentException>(() => handler.HandleAsync(new CreateCustomerCommand(
            garage.Id,
            "Maria",
            "529A982B247C25",
            "11999999999",
            "maria@test.local")));

        customers.Added.ShouldBeNull();
        unitOfWork.SaveCalls.ShouldBe(0);
    }

    private sealed class CustomerRepository : ICustomerRepository
    {
        public CustomerEntity? Added { get; private set; }
        public Task AddAsync(CustomerEntity customer, CancellationToken cancellationToken = default)
        {
            Added = customer;
            return Task.CompletedTask;
        }
        public Task<bool> ExistsByDocumentAsync(Guid garageId, string document, Guid? excludingCustomerId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task<CustomerEntity?> GetByIdAsync(Guid id, Guid garageId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CustomerEntity?>(null);
    }

    private sealed class GarageRepository(GarageEntity garage) : IGarageRepository
    {
        public Task AddAsync(GarageEntity value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsByDocumentAsync(string document, Guid? excludingGarageId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<GarageEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<GarageEntity?>(id == garage.Id ? garage : null);
        public Task<GarageEntity?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => GetByIdAsync(id, cancellationToken);
        public Task<IReadOnlyList<GarageEntity>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GarageEntity>>([garage]);
    }

    private sealed class UnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }
}
