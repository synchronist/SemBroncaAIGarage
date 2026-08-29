using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Vehicles.CreateVehicle;
using SemBroncaAI.Garage.Domain.Entities.Customer;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;
using SemBroncaAI.Garage.Domain.Interfaces;
using Shouldly;
namespace SemBroncaAI.Garage.Tests.Application.Vehicles;
public sealed class CreateVehicleHandlerTests
{
    [Fact] public async Task Should_Reject_Customer_From_Another_Garage()
    {
        var garageId=Guid.CreateVersion7();var handler=new CreateVehicleHandler(new GarageRepo(new GarageEntity("Oficina","123","1199","a@b.com")),new CustomerRepo(null),new VehicleRepo(false),new UnitOfWork());
        await Should.ThrowAsync<InvalidOperationException>(()=>handler.HandleAsync(Command(garageId)));
    }
    [Fact] public async Task Should_Reject_Duplicate_Plate_In_Same_Garage()
    {
        var garage=new GarageEntity("Oficina","123","1199","a@b.com");var customer=new CustomerEntity(garage.Id,"Cliente","","11999999999","c@d.com");var handler=new CreateVehicleHandler(new GarageRepo(garage),new CustomerRepo(customer),new VehicleRepo(true),new UnitOfWork());
        var error=await Should.ThrowAsync<InvalidOperationException>(()=>handler.HandleAsync(Command(garage.Id,customer.Id)));error.Message.ShouldContain("placa");
    }
    private static CreateVehicleCommand Command(Guid garageId,Guid? customerId=null)=>new(garageId,customerId??Guid.CreateVersion7(),"abc-1234","Fiat","Uno","",2020,"Prata","Flex",0);
    private sealed class GarageRepo(GarageEntity garage):IGarageRepository{public Task<GarageEntity?> GetByIdAsync(Guid id,CancellationToken t=default)=>Task.FromResult<GarageEntity?>(id==garage.Id?garage:null);public Task<GarageEntity?> GetForUpdateAsync(Guid id,CancellationToken t=default)=>GetByIdAsync(id,t);public Task AddAsync(GarageEntity g,CancellationToken t=default)=>Task.CompletedTask;public Task<bool> ExistsByDocumentAsync(string d,Guid? e=null,CancellationToken t=default)=>Task.FromResult(false);public Task<IReadOnlyList<GarageEntity>> GetAllAsync(CancellationToken t=default)=>Task.FromResult<IReadOnlyList<GarageEntity>>([]);}
    private sealed class CustomerRepo(CustomerEntity? customer):ICustomerRepository{public Task<CustomerEntity?> GetByIdAsync(Guid id,Guid garageId,CancellationToken t=default)=>Task.FromResult<CustomerEntity?>(customer is not null&&customer.Id==id&&customer.GarageId==garageId?customer:null);public Task AddAsync(CustomerEntity c,CancellationToken t=default)=>Task.CompletedTask;public Task<bool> ExistsByDocumentAsync(Guid g,string d,Guid? e=null,CancellationToken t=default)=>Task.FromResult(false);}
    private sealed class VehicleRepo(bool exists):IVehicleRepository{public Task AddAsync(VehicleEntity v,CancellationToken t)=>Task.CompletedTask;public Task<VehicleEntity?> GetByIdAsync(Guid id,Guid g,CancellationToken t)=>Task.FromResult<VehicleEntity?>(null);public Task<bool> ExistsByPlateAsync(Guid g,string p,Guid? e=null,CancellationToken t=default)=>Task.FromResult(exists);}
    private sealed class UnitOfWork:IUnitOfWork{public Task<int> SaveChangesAsync(CancellationToken t=default)=>Task.FromResult(1);}
}
