using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Vehicles.GetVehicleById;
using SemBroncaAI.Garage.Application.Features.Vehicles.ListVehicles;
using Shouldly;
namespace SemBroncaAI.Garage.Tests.Application.Vehicles;
public sealed class ListVehiclesHandlerTests
{
    [Fact] public async Task Should_Preserve_Garage_Boundary(){var id=Guid.CreateVersion7();var spy=new Spy();await new ListVehiclesHandler(spy).HandleAsync(new(id,"ABC",1,20));spy.Query!.GarageId.ShouldBe(id);}
    private sealed class Spy:IVehicleQueryRepository
    {public ListVehiclesQuery? Query{get;private set;}public Task<ListVehiclesResponse> ListAsync(ListVehiclesQuery q,CancellationToken t=default){Query=q;return Task.FromResult(new ListVehiclesResponse(1,20,0,0,[]));}public Task<GetVehicleByIdResponse?> GetByIdAsync(Guid id,Guid garageId,CancellationToken t=default)=>Task.FromResult<GetVehicleByIdResponse?>(null);}
}
