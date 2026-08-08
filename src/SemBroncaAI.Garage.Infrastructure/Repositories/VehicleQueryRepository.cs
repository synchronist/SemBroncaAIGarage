using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Vehicles.GetVehicleById;
using SemBroncaAI.Garage.Application.Features.Vehicles.ListVehicles;
using SemBroncaAI.Garage.Infrastructure.Persistence;
namespace SemBroncaAI.Garage.Infrastructure.Repositories;
public sealed class VehicleQueryRepository(GarageDbContext context) : IVehicleQueryRepository
{
    public async Task<ListVehiclesResponse> ListAsync(ListVehiclesQuery query, CancellationToken cancellationToken = default)
    {
        var vehicles = context.Vehicles.AsNoTracking().Where(x => x.GarageId == query.GarageId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            vehicles = vehicles.Where(x => EF.Functions.ILike(x.Plate, term) || EF.Functions.ILike(x.Brand, term) || EF.Functions.ILike(x.Model, term) || EF.Functions.ILike(x.Customer.Name, term) || EF.Functions.ILike(x.Customer.Phone, term) || EF.Functions.ILike(x.Customer.Document, term));
        }
        var total = await vehicles.CountAsync(cancellationToken);
        var pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.PageSize);
        var items = await vehicles.OrderBy(x => x.Plate).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new ListVehiclesItem(x.Id, x.Plate, x.Brand, x.Model, x.Version, x.Year, x.Color, x.Fuel, x.Mileage, x.CustomerId, x.Customer.Name, x.Active)).ToArrayAsync(cancellationToken);
        return new ListVehiclesResponse(query.Page, query.PageSize, total, pages, items);
    }

    public Task<GetVehicleByIdResponse?> GetByIdAsync(Guid id, Guid garageId, CancellationToken cancellationToken = default) =>
        context.Vehicles.AsNoTracking().Where(x => x.Id == id && x.GarageId == garageId)
            .Select(x => new GetVehicleByIdResponse(x.Id, x.GarageId, x.Plate, x.Brand, x.Model, x.Version, x.Year, x.Color, x.Fuel, x.Mileage, x.Active, x.CreatedAt,
                new VehicleCustomerResponse(x.Customer.Id, x.Customer.Name, x.Customer.Document, x.Customer.Phone, x.Customer.Email),
                x.ServiceOrders.OrderByDescending(o => o.CreatedAt).Select(o => new VehicleServiceOrderResponse(o.Id, o.Number, o.Status, o.CustomerComplaint, o.Mileage, o.CreatedAt)).ToArray()))
            .FirstOrDefaultAsync(cancellationToken);
}
