using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.ListServiceOrders;
using SemBroncaAI.Garage.Infrastructure.Persistence;

namespace SemBroncaAI.Garage.Infrastructure.Repositories;

public sealed class ServiceOrderQueryRepository
    : IServiceOrderQueryRepository
{
    private readonly GarageDbContext _context;

    public ServiceOrderQueryRepository(
        GarageDbContext context)
    {
        _context = context;
    }

    public async Task<ListServiceOrdersResponse> ListAsync(
        ListServiceOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        var serviceOrders =
            _context.ServiceOrders
                .AsNoTracking()
                .ApplyTenantAndArchiveFilter(query.GarageId, query.Archive);

        if (query.Status.HasValue)
        {
            serviceOrders =
                serviceOrders.Where(serviceOrder =>
                    serviceOrder.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            var searchTerm = $"%{search}%";

            var isServiceOrderNumber =
                int.TryParse(search, out var serviceOrderNumber);

            serviceOrders =
                serviceOrders.Where(serviceOrder =>
                    EF.Functions.ILike(
                        serviceOrder.Vehicle.Plate,
                        searchTerm) ||

                    EF.Functions.ILike(
                        serviceOrder.Vehicle.Brand,
                        searchTerm) ||

                    EF.Functions.ILike(
                        serviceOrder.Vehicle.Model,
                        searchTerm) ||

                    EF.Functions.ILike(
                        serviceOrder.Vehicle.Customer.Name,
                        searchTerm) ||

                    EF.Functions.ILike(
                        serviceOrder.Vehicle.Customer.Phone,
                        searchTerm) ||

                    (
                        isServiceOrderNumber &&
                        serviceOrder.Number ==
                        serviceOrderNumber
                    ));
        }

        var totalItems =
            await serviceOrders.CountAsync(
                cancellationToken);

        var totalPages =
            totalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    totalItems /
                    (double)query.PageSize);

        var items =
            await serviceOrders
                .OrderByDescending(serviceOrder =>
                    serviceOrder.CreatedAt)
                .Skip(
                    (query.Page - 1) *
                    query.PageSize)
                .Take(query.PageSize)
                .Select(serviceOrder =>
                    new ListServiceOrdersItem(
                        serviceOrder.Id,
                        serviceOrder.Number,
                        serviceOrder.Status,
                        serviceOrder.ArchivedAt,
                        serviceOrder.CreatedAt,
                        serviceOrder.CustomerComplaint,
                        serviceOrder.Vehicle.Customer.Id,
                        serviceOrder.Vehicle.Customer.Name,
                        serviceOrder.Vehicle.Customer.Phone,
                        serviceOrder.Vehicle.Id,
                        serviceOrder.Vehicle.Plate,
                        serviceOrder.Vehicle.Brand,
                        serviceOrder.Vehicle.Model,
                        serviceOrder.Vehicle.Version,
                        serviceOrder.Vehicle.Year))
                .ToArrayAsync(
                    cancellationToken);

        return new ListServiceOrdersResponse(
            query.Page,
            query.PageSize,
            totalItems,
            totalPages,
            items);
    }
}
