using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Domain.Entities.Customer;

namespace SemBroncaAI.Garage.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly GarageDbContext _dbContext;

    public CustomerRepository(GarageDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        CustomerEntity customer,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Customers.AddAsync(
            customer,
            cancellationToken);
    }

    public Task<bool> ExistsByDocumentAsync(
        Guid garageId,
        string document,
        Guid? excludingCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Customers.AnyAsync(
            customer =>
                customer.GarageId == garageId &&
                customer.Document == document &&
                (!excludingCustomerId.HasValue || customer.Id != excludingCustomerId.Value),
            cancellationToken);
    }

    public async Task<CustomerEntity?> GetByIdAsync(
        Guid id,
        Guid garageId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .FirstOrDefaultAsync(
                customer => customer.Id == id && customer.GarageId == garageId,
                cancellationToken);
    }
}
