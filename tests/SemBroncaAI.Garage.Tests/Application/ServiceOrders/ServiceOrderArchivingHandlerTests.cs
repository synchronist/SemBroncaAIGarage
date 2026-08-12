using SemBroncaAI.Garage.Application.Features.ServiceOrders.ArchiveServiceOrder;
using SemBroncaAI.Garage.Application.Features.ServiceOrders.RestoreServiceOrder;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Interfaces;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Application.ServiceOrders;

public sealed class ServiceOrderArchivingHandlerTests
{
    [Fact]
    public async Task Archive_should_validate_garage_boundary()
    {
        var order = CancelledOrder(); var unit = new UnitOfWork();
        var handler = new ArchiveServiceOrderHandler(new Repository(order), unit);
        await Should.ThrowAsync<InvalidOperationException>(() => handler.HandleAsync(order.Id, Guid.NewGuid()));
        order.ArchivedAt.ShouldBeNull(); unit.Saves.ShouldBe(0);
    }

    [Fact]
    public async Task Archive_and_restore_should_persist_without_changing_status()
    {
        var order = CancelledOrder(); var unit = new UnitOfWork(); var repository = new Repository(order);
        await new ArchiveServiceOrderHandler(repository, unit).HandleAsync(order.Id, order.GarageId);
        order.ArchivedAt.ShouldNotBeNull(); order.Status.ShouldBe(ServiceOrderStatus.Cancelled);
        await new RestoreServiceOrderHandler(repository, unit).HandleAsync(order.Id, order.GarageId);
        order.ArchivedAt.ShouldBeNull(); order.Status.ShouldBe(ServiceOrderStatus.Cancelled); unit.Saves.ShouldBe(2);
    }

    private static ServiceOrderEntity CancelledOrder() { var order = new ServiceOrderEntity(Guid.NewGuid(), Guid.NewGuid(), 1, "Revisão", 10); order.Cancel(); return order; }
    private sealed class Repository(ServiceOrderEntity order) : IServiceOrderRepository
    {
        public Task<ServiceOrderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ServiceOrderEntity?>(id == order.Id ? order : null);
        public Task<ServiceOrderEntity?> GetByNumberAsync(Guid garageId, int number, CancellationToken cancellationToken = default) => Task.FromResult<ServiceOrderEntity?>(null);
        public Task AddAsync(ServiceOrderEntity serviceOrder, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void RemoveEstimateItems(IEnumerable<ServiceOrderEstimateItemEntity> items) { }
    }
    private sealed class UnitOfWork : IUnitOfWork { public int Saves { get; private set; } public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { Saves++; return Task.FromResult(1); } }
}
