using SemBroncaAI.Garage.Application.Features.ServiceOrders.GetTechnicalHistory;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Interfaces;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Application.ServiceOrders;

public sealed class GetTechnicalHistoryHandlerTests
{
    [Theory]
    [InlineData(0, 2, 2)]
    [InlineData(2, 5, 5)]
    [InlineData(7, 5, 5)]
    public async Task Should_return_requested_page_without_repeating_current_order(
        int offset, int pageSize, int expectedCount)
    {
        var garageId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var current = new ServiceOrderEntity(garageId, vehicleId, 99, "Atual", 100);
        var history = Enumerable.Range(1, 12)
            .Select(number => new ServiceOrderEntity(garageId, vehicleId, number, $"Relato {number}", number))
            .OrderByDescending(item => item.Number).ToArray();
        var repository = new Repository(current, history);

        var result = await new GetTechnicalHistoryHandler(repository)
            .HandleAsync(current.Id, garageId, offset, pageSize);

        result.TotalCount.ShouldBe(12);
        result.Items.Count.ShouldBe(expectedCount);
        result.Items.ShouldNotContain(item => item.Id == current.Id);
        result.Items.Select(item => item.Id).Distinct().Count().ShouldBe(expectedCount);
        repository.GarageId.ShouldBe(garageId);
        repository.ExcludedId.ShouldBe(current.Id);
    }

    [Fact]
    public async Task Empty_history_should_return_zero_total_and_items()
    {
        var current = new ServiceOrderEntity(Guid.NewGuid(), Guid.NewGuid(), 1, "Atual", 0);
        var result = await new GetTechnicalHistoryHandler(new Repository(current, []))
            .HandleAsync(current.Id, current.GarageId, 0, 2);
        result.TotalCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    private sealed class Repository(ServiceOrderEntity current, IReadOnlyCollection<ServiceOrderEntity> history)
        : IServiceOrderRepository
    {
        public Guid GarageId { get; private set; }
        public Guid ExcludedId { get; private set; }
        public Task<ServiceOrderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ServiceOrderEntity?>(id == current.Id ? current : null);
        public Task<ServiceOrderEntity?> GetByIdAsync(Guid id, Guid garageId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ServiceOrderEntity?>(id == current.Id && garageId == current.GarageId ? current : null);
        public Task<ServiceOrderTechnicalHistoryPage> ListVehicleTechnicalHistoryAsync(
            Guid garageId, Guid vehicleId, Guid excludeServiceOrderId, int offset, int pageSize,
            CancellationToken cancellationToken = default)
        {
            GarageId = garageId;
            ExcludedId = excludeServiceOrderId;
            return Task.FromResult(new ServiceOrderTechnicalHistoryPage(
                history.Count, history.Skip(offset).Take(pageSize).ToArray()));
        }
        public Task<ServiceOrderEntity?> GetByNumberAsync(Guid garageId, int number, CancellationToken cancellationToken = default) => Task.FromResult<ServiceOrderEntity?>(null);
        public Task AddAsync(ServiceOrderEntity serviceOrder, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void RemoveEstimateItems(IEnumerable<ServiceOrderEstimateItemEntity> items) { }
    }
}
