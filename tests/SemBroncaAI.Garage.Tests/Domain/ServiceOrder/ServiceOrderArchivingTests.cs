using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Domain.ServiceOrder;

public sealed class ServiceOrderArchivingTests
{
    [Fact]
    public void Should_archive_cancelled_order_without_changing_status_or_data()
    {
        var order = CreateOrder();
        order.Cancel();
        var history = order.History.ToArray();
        var archivedAt = DateTimeOffset.UtcNow;

        order.Archive(archivedAt);

        order.ArchivedAt.ShouldBe(archivedAt);
        order.Status.ShouldBe(ServiceOrderStatus.Cancelled);
        order.CustomerComplaint.ShouldBe("Barulho na suspensão");
        order.Mileage.ShouldBe(35000);
        order.VehicleId.ShouldNotBe(Guid.Empty);
        order.History.ShouldBe(history);
    }

    [Fact]
    public void Should_archive_delivered_order()
    {
        var order = CreateDeliveredOrder();
        order.Archive(DateTimeOffset.UtcNow);
        order.ArchivedAt.ShouldNotBeNull();
        order.Status.ShouldBe(ServiceOrderStatus.Delivered);
    }

    [Theory]
    [InlineData(ServiceOrderStatus.Received)]
    [InlineData(ServiceOrderStatus.Diagnosis)]
    [InlineData(ServiceOrderStatus.WaitingApproval)]
    [InlineData(ServiceOrderStatus.InProgress)]
    [InlineData(ServiceOrderStatus.WaitingParts)]
    [InlineData(ServiceOrderStatus.Finished)]
    public void Should_not_archive_non_terminal_order(ServiceOrderStatus status)
    {
        var order = CreateInStatus(status);
        Should.Throw<InvalidOperationException>(() => order.Archive(DateTimeOffset.UtcNow));
        order.ArchivedAt.ShouldBeNull();
        order.Status.ShouldBe(status);
    }

    [Fact]
    public void Archive_and_restore_should_be_idempotent()
    {
        var order = CreateOrder(); order.Cancel();
        var first = DateTimeOffset.UtcNow;
        order.Archive(first); order.Archive(first.AddHours(1));
        order.ArchivedAt.ShouldBe(first);

        order.Restore(); order.Restore();
        order.ArchivedAt.ShouldBeNull();
        order.Status.ShouldBe(ServiceOrderStatus.Cancelled);
    }

    [Fact]
    public void Archived_order_should_remain_in_vehicle_history_relationship()
    {
        var garageId = Guid.NewGuid();
        var vehicle = new VehicleEntity(garageId, Guid.NewGuid(), "ABC1D23", "VW", "Gol", "", 2020, "Prata", "Flex", 100);
        var order = new ServiceOrderEntity(garageId, vehicle.Id, 1, "OS", 100);
        vehicle.ServiceOrders.Add(order);
        order.Cancel();

        order.Archive(DateTimeOffset.UtcNow);

        vehicle.ServiceOrders.ShouldContain(order);
        vehicle.ServiceOrders.Single().ArchivedAt.ShouldNotBeNull();
    }

    private static ServiceOrderEntity CreateOrder() => new(Guid.NewGuid(), Guid.NewGuid(), 1, "Barulho na suspensão", 35000);
    private static ServiceOrderEntity CreateDeliveredOrder()
    {
        var order = CreateInStatus(ServiceOrderStatus.Finished); order.Deliver(); return order;
    }
    private static ServiceOrderEntity CreateInStatus(ServiceOrderStatus target)
    {
        var order = CreateOrder();
        if (target == ServiceOrderStatus.Received) return order;
        order.StartDiagnosis();
        if (target == ServiceOrderStatus.Diagnosis) return order;
        order.SaveDiagnosis("Diagnóstico");
        order.SaveEstimate([new("Serviço", EstimateItemType.Service, 1, 100)]);
        var now = DateTimeOffset.UtcNow;
        var approval = order.SendForApproval(Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), "protected", now.AddDays(1), now);
        if (target == ServiceOrderStatus.WaitingApproval) return order;
        order.ApproveEstimate(approval.Id, "Cliente", now.AddMinutes(1)); order.StartService();
        if (target == ServiceOrderStatus.InProgress) return order;
        if (target == ServiceOrderStatus.WaitingParts) { order.WaitForParts(); return order; }
        order.Finish(); return order;
    }
}
