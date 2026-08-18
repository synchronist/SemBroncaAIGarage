using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Domain.Common;
using SemBroncaAI.Garage.Application.Common;
namespace SemBroncaAI.Garage.Application.Features.Vehicles.ListVehicles;
public sealed class ListVehiclesHandler(IVehicleQueryRepository repository)
{
    public Task<ListVehiclesResponse> HandleAsync(ListVehiclesQuery query, CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmpty(query.GarageId, nameof(query.GarageId));
        PaginationRules.Validate(query.Page, query.PageSize);
        return repository.ListAsync(query, cancellationToken);
    }
}
