using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Customers.GetCustomerById;
using SemBroncaAI.Garage.Application.Features.Customers.ListCustomers;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Infrastructure.Repositories;

public sealed class CustomerQueryRepository : ICustomerQueryRepository
{
    private readonly GarageDbContext _context;
    public CustomerQueryRepository(GarageDbContext context) => _context = context;

    public async Task<ListCustomersResponse> ListAsync(ListCustomersQuery query, CancellationToken cancellationToken = default)
    {
        var customers = _context.Customers.AsNoTracking().Where(x => x.GarageId == query.GarageId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            customers = customers.Where(x => EF.Functions.ILike(x.Name, term) || EF.Functions.ILike(x.Document, term) || EF.Functions.ILike(x.Phone, term) || EF.Functions.ILike(x.Email, term));
        }

        var totalItems = await customers.CountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)query.PageSize);
        var items = await customers.OrderBy(x => x.Name)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new ListCustomersItem(x.Id, x.Name, x.Document, x.Phone, x.Email, x.Active, x.CreatedAt, x.Vehicles.Count))
            .ToArrayAsync(cancellationToken);
        return new ListCustomersResponse(query.Page, query.PageSize, totalItems, totalPages, items);
    }

    public Task<GetCustomerByIdResponse?> GetByIdAsync(Guid id, Guid garageId, CancellationToken cancellationToken = default)
    {
        return _context.Customers.AsNoTracking()
            .Where(x => x.Id == id && x.GarageId == garageId)
            .Select(x => new GetCustomerByIdResponse(x.Id, x.GarageId, x.Name, x.Document, x.Phone, x.Email, x.Active, x.CreatedAt,
                x.Vehicles.OrderBy(v => v.Plate).Select(v => new CustomerVehicleResponse(v.Id, v.Plate, v.Brand, v.Model, v.Version, v.Year, v.Color, v.Fuel, v.Mileage, v.Active)).ToArray()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
