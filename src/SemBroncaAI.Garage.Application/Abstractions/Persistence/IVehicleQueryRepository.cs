using SemBroncaAI.Garage.Application.Features.Vehicles.GetVehicleById;
using SemBroncaAI.Garage.Application.Features.Vehicles.ListVehicles;
namespace SemBroncaAI.Garage.Application.Abstractions.Persistence;
public interface IVehicleQueryRepository
{
    Task<ListVehiclesResponse> ListAsync(ListVehiclesQuery query, CancellationToken cancellationToken = default);
    Task<GetVehicleByIdResponse?> GetByIdAsync(Guid id, Guid garageId, CancellationToken cancellationToken = default);
}
