using System.Reflection;
using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.CreateServiceOrder;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.GetServiceOrderById;
using SemBroncaAI.Garage.Domain.Entities.Customer;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;
using SemBroncaAI.Garage.Domain.Interfaces;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Application.ServiceOrders;

public sealed class ServiceOrderMileageTests
{
    [Fact]
    public async Task Create_Should_Preserve_Order_Mileage_Update_Vehicle_And_Save_Once()
    {
        var garage = new GarageEntity("Oficina", "123", "1199", "a@b.com");
        var customer = new CustomerEntity(garage.Id, "Cliente", "", "11999999999", "c@d.com");
        var vehicle = new VehicleEntity(garage.Id, customer.Id, "ABC1234", "Fiat", "Uno", "", 2020, "Prata", "Flex", 30000);
        var orders = new OrderRepository();
        var unitOfWork = new UnitOfWork();
        var handler = new CreateServiceOrderHandler(new GarageRepository(garage), new VehicleRepository(vehicle), orders, new NumberGenerator(), unitOfWork);

        await handler.HandleAsync(new CreateServiceOrderCommand(garage.Id, vehicle.Id, "Revisão", 36250));

        orders.Added.ShouldNotBeNull();
        orders.Added.Mileage.ShouldBe(36250);
        vehicle.Mileage.ShouldBe(36250);
        unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetById_Should_Return_Order_Mileage_Instead_Of_Current_Vehicle_Mileage()
    {
        var garage = new GarageEntity("Oficina", "123", "1199", "a@b.com");
        var customer = new CustomerEntity(garage.Id, "Cliente", "", "11999999999", "c@d.com");
        var vehicle = new VehicleEntity(garage.Id, customer.Id, "ABC1234", "Fiat", "Uno", "", 2020, "Prata", "Flex", 35000);
        SetNavigation(vehicle, "Customer", customer);
        var order = new ServiceOrderEntity(garage.Id, vehicle.Id, 1, "Revisão", 35000);
        SetNavigation(order, "Vehicle", vehicle);
        vehicle.UpdateMileage(42500);

        var response = await new GetServiceOrderByIdHandler(new OrderRepository(order)).HandleAsync(order.Id);

        response.ShouldNotBeNull();
        response.Mileage.ShouldBe(35000);
        response.Vehicle.Mileage.ShouldBe(42500);
    }

    [Fact]
    public async Task GetById_Should_Represent_Historical_Order_Without_Mileage_As_Unknown()
    {
        var garage = new GarageEntity("Oficina", "123", "1199", "a@b.com");
        var customer = new CustomerEntity(garage.Id, "Cliente", "", "11999999999", "c@d.com");
        var vehicle = new VehicleEntity(garage.Id, customer.Id, "ABC1234", "Fiat", "Uno", "", 2020, "Prata", "Flex", 42500);
        SetNavigation(vehicle, "Customer", customer);
        var order = new ServiceOrderEntity(garage.Id, vehicle.Id, 1, "OS antiga", 0);
        SetNavigation(order, "Vehicle", vehicle);
        SetNavigation(order, "Mileage", null!);

        var response = await new GetServiceOrderByIdHandler(new OrderRepository(order)).HandleAsync(order.Id);

        response.ShouldNotBeNull();
        response.Mileage.ShouldBeNull();
    }

    private static void SetNavigation(object target, string property, object value) =>
        target.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public)!.SetValue(target, value);

    private sealed class GarageRepository(GarageEntity garage) : IGarageRepository
    {
        public Task<GarageEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<GarageEntity?>(id == garage.Id ? garage : null);
        public Task AddAsync(GarageEntity entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsByDocumentAsync(string document, Guid? excludingGarageId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<GarageEntity?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => GetByIdAsync(id, cancellationToken);
        public Task<IReadOnlyList<GarageEntity>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GarageEntity>>([]);
    }

    private sealed class VehicleRepository(VehicleEntity vehicle) : IVehicleRepository
    {
        public Task<VehicleEntity?> GetByIdAsync(Guid id, Guid garageId, CancellationToken cancellationToken = default) => Task.FromResult<VehicleEntity?>(id == vehicle.Id && garageId == vehicle.GarageId ? vehicle : null);
        public Task AddAsync(VehicleEntity entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsByPlateAsync(Guid garageId, string plate, Guid? excludingVehicleId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class OrderRepository(ServiceOrderEntity? existing = null) : IServiceOrderRepository
    {
        public ServiceOrderEntity? Added { get; private set; }
        public Task AddAsync(ServiceOrderEntity serviceOrder, CancellationToken cancellationToken = default) { Added = serviceOrder; return Task.CompletedTask; }
        public Task<ServiceOrderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(existing?.Id == id ? existing : null);
        public Task<ServiceOrderEntity?> GetByNumberAsync(Guid garageId, int number, CancellationToken cancellationToken = default) => Task.FromResult<ServiceOrderEntity?>(null);
        public void RemoveEstimateItems(IEnumerable<ServiceOrderEstimateItemEntity> items) { }
    }

    private sealed class NumberGenerator : IServiceOrderNumberGenerator
    {
        public Task<int> GetNextAsync(Guid garageId, CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class UnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { SaveCount++; return Task.FromResult(1); }
    }
}
