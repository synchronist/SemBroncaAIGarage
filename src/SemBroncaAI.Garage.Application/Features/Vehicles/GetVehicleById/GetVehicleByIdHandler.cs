using SemBroncaAI.Garage.Application.Abstractions.Persistence;
namespace SemBroncaAI.Garage.Application.Features.Vehicles.GetVehicleById;
public sealed class GetVehicleByIdHandler(IVehicleQueryRepository repository)
{
    public Task<GetVehicleByIdResponse?> HandleAsync(Guid id, Guid garageId, CancellationToken cancellationToken = default) => repository.GetByIdAsync(id, garageId, cancellationToken);
}
