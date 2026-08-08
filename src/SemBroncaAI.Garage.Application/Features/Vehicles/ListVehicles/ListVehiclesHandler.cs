using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Domain.Common;
namespace SemBroncaAI.Garage.Application.Features.Vehicles.ListVehicles;
public sealed class ListVehiclesHandler(IVehicleQueryRepository repository)
{
    public Task<ListVehiclesResponse> HandleAsync(ListVehiclesQuery query, CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmpty(query.GarageId, nameof(query.GarageId));
        return repository.ListAsync(query with { Page = Math.Max(1, query.Page), PageSize = Math.Clamp(query.PageSize, 1, 100) }, cancellationToken);
    }
}
