using SemBroncaAI.Garage.Domain.Entities.Vehicle;
using Shouldly;
namespace SemBroncaAI.Garage.Tests.Domain.Vehicle;
public sealed class VehicleEntityTests
{
    [Fact] public void Should_Create_And_Normalize_Valid_Vehicle(){var v=Create("abc-1d23",10);v.Plate.ShouldBe("ABC1D23");v.Mileage.ShouldBe(10);v.Active.ShouldBeTrue();}
    [Fact] public void Should_Update_Vehicle(){var v=Create();var customer=Guid.CreateVersion7();v.Update(customer,"xyz 9a99","Ford","Ka","SE",2020,"Prata","Flex",25000);v.CustomerId.ShouldBe(customer);v.Plate.ShouldBe("XYZ9A99");v.Brand.ShouldBe("Ford");}
    [Fact] public void Should_Reject_Empty_Garage(){Should.Throw<ArgumentException>(()=>new VehicleEntity(Guid.Empty,Guid.CreateVersion7(),"ABC1234","Fiat","Uno","",2020,"","",0));}
    [Fact] public void Should_Reject_Empty_Customer(){Should.Throw<ArgumentException>(()=>new VehicleEntity(Guid.CreateVersion7(),Guid.Empty,"ABC1234","Fiat","Uno","",2020,"","",0));}
    [Fact] public void Should_Reject_Empty_Plate(){Should.Throw<ArgumentException>(()=>Create(""));}
    [Fact] public void Should_Reject_Negative_Mileage(){Should.Throw<ArgumentOutOfRangeException>(()=>Create(mileage:-1));}
    [Fact] public void Should_Update_Mileage(){var v=Create();v.UpdateMileage(12345);v.Mileage.ShouldBe(12345);}
    private static VehicleEntity Create(string plate="ABC1234",int mileage=0)=>new(Guid.CreateVersion7(),Guid.CreateVersion7(),plate,"Fiat","Uno","Mille",2020,"Prata","Flex",mileage);
}
