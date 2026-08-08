using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Lookup;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Infrastructure.Repositories;

public sealed class LookupRepository : ILookupRepository
{
    private readonly GarageDbContext _dbContext;

    public LookupRepository(
        GarageDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<LookupResultResponse>> SearchAsync(
        Guid garageId,
        string query,
        int limit = 8,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = $"%{query}%";

        var hasServiceOrderNumber =
            int.TryParse(query, out var serviceOrderNumber);

        var results = await _dbContext.Vehicles
            .AsNoTracking()
            .Where(vehicle =>
                vehicle.GarageId == garageId &&
                vehicle.Active &&
                (
                    EF.Functions.ILike(
                        vehicle.Plate,
                        searchTerm) ||

                    EF.Functions.ILike(
                        vehicle.Brand,
                        searchTerm) ||

                    EF.Functions.ILike(
                        vehicle.Model,
                        searchTerm) ||

                    EF.Functions.ILike(
                        vehicle.Customer.Name,
                        searchTerm) ||

                    EF.Functions.ILike(
                        vehicle.Customer.Phone,
                        searchTerm) ||

                    EF.Functions.ILike(
                        vehicle.Customer.Document,
                        searchTerm) ||

                    (
                        hasServiceOrderNumber &&
                        vehicle.ServiceOrders.Any(
                            serviceOrder =>
                                serviceOrder.Number ==
                                serviceOrderNumber)
                    )
                ))
            .OrderBy(vehicle => vehicle.Plate)
            .Take(limit)
            .Select(vehicle => new LookupResultResponse(
                vehicle.Id,
                vehicle.CustomerId,
                vehicle.GarageId,
                vehicle.Plate,
                vehicle.Brand,
                vehicle.Model,
                vehicle.Version,
                vehicle.Year,
                vehicle.Color,
                vehicle.Fuel,
                vehicle.Mileage,
                vehicle.Customer.Name,
                vehicle.Customer.Phone,
                vehicle.Customer.Document,
                vehicle.Customer.Email))
            .ToListAsync(cancellationToken);

        return results;
    }
}