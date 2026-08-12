using Shouldly;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Tests.Domain.ServiceOrder;

public class ServiceOrderEntityTests
{
    [Fact]
    public void Should_Create_Service_Order()
    {
        // Arrange
        var garageId = Guid.CreateVersion7();
        var vehicleId = Guid.CreateVersion7();

        // Act
        var order = new ServiceOrderEntity(
            garageId,
            vehicleId,
            1,
            "Barulho na suspensão",
            35000);

        // Assert
        order.GarageId.ShouldBe(garageId);
        order.VehicleId.ShouldBe(vehicleId);
        order.Number.ShouldBe(1);
        order.Status.ShouldBe(ServiceOrderStatus.Received);
        order.CustomerComplaint.ShouldBe("Barulho na suspensão");
        order.Mileage.ShouldBe(35000);

        order.History.Count.ShouldBe(1);
    }

    [Fact]
    public void Should_Reject_Negative_Mileage()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ServiceOrderEntity(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            "Barulho na suspensão",
            -1));
    }

    [Fact]
    public void Should_Preserve_Mileage_When_Vehicle_Mileage_Changes()
    {
        var garageId = Guid.CreateVersion7();
        var vehicle = new SemBroncaAI.Garage.Domain.Entities.Vehicle.VehicleEntity(
            garageId, Guid.CreateVersion7(), "ABC1234", "Fiat", "Uno", "", 2020, "Prata", "Flex", 35000);
        var order = new ServiceOrderEntity(garageId, vehicle.Id, 1, "Revisão", vehicle.Mileage);

        vehicle.UpdateMileage(42500);

        order.Mileage.ShouldBe(35000);
        vehicle.Mileage.ShouldBe(42500);
    }

    [Fact]
    public void Should_Start_Diagnosis_And_Add_History()
    {
        var order = new ServiceOrderEntity(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            "Barulho na suspensão",
            35000);

        order.StartDiagnosis();

        order.Status.ShouldBe(ServiceOrderStatus.Diagnosis);
        order.History.Count.ShouldBe(2);

        var lastHistory = order.History.Last();

        lastHistory.PreviousStatus.ShouldBe(
            ServiceOrderStatus.Received);

        lastHistory.CurrentStatus.ShouldBe(
            ServiceOrderStatus.Diagnosis);

        lastHistory.Description.ShouldBe(
            ServiceOrderMessages.DiagnosisStarted);
    }

    [Fact]
    public void Should_Save_Diagnosis()
    {
        var order = new ServiceOrderEntity(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            "Barulho na suspensão",
            35000);

        order.StartDiagnosis();

        order.SaveDiagnosis(
            "Folga identificada na bieleta dianteira direita.",
            "Verificar também as buchas da bandeja.");

        order.Diagnosis.ShouldNotBeNull();

        order.Diagnosis.Description.ShouldBe(
            "Folga identificada na bieleta dianteira direita.");

        order.Diagnosis.InternalNotes.ShouldBe(
            "Verificar também as buchas da bandeja.");
    }

    [Fact]
    public void Should_Send_Service_Order_For_Approval()
    {
        var order = new ServiceOrderEntity(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            "Barulho na suspensão",
            35000);

        order.StartDiagnosis();

        order.SaveDiagnosis(
            "Folga identificada na bieleta dianteira direita.",
            "Verificar também as buchas da bandeja.");

        order.SaveEstimate([
            new ServiceOrderEstimateItemData(
                "Troca da bieleta",
                EstimateItemType.Service,
                1,
                150m)
        ]);

        SendForApproval(order);

        order.Status.ShouldBe(
            ServiceOrderStatus.WaitingApproval);

        order.History.Count.ShouldBe(3);

        var lastHistory = order.History.Last();

        lastHistory.PreviousStatus.ShouldBe(
            ServiceOrderStatus.Diagnosis);

        lastHistory.CurrentStatus.ShouldBe(
            ServiceOrderStatus.WaitingApproval);

        lastHistory.Description.ShouldBe(
            ServiceOrderMessages.SentForApproval);
    }

    [Fact]
    public void Should_Not_Send_Service_Order_For_Approval_Without_Diagnosis()
    {
        var order = new ServiceOrderEntity(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            "Barulho na suspensão",
            35000);

        order.StartDiagnosis();

        var exception = Should.Throw<InvalidOperationException>(
            () => SendForApproval(order));

        exception.Message.ShouldBe(
            "Registre o diagnóstico antes de enviar a ordem para aprovação.");

        order.Status.ShouldBe(
            ServiceOrderStatus.Diagnosis);
    }

    [Fact]
    public void Should_Create_Valid_Estimate_And_Calculate_Totals()
    {
        var order = CreateOrderInDiagnosisWithDiagnosis();

        order.SaveEstimate([
            new ServiceOrderEstimateItemData("Mão de obra", EstimateItemType.Service, 2, 125m),
            new ServiceOrderEstimateItemData("Filtro de óleo", EstimateItemType.Part, 2, 35.50m),
            new ServiceOrderEstimateItemData("Óleo do motor", EstimateItemType.Part, 4, 42m)
        ]);

        order.Estimate.ShouldNotBeNull();
        order.Estimate.Items.Count.ShouldBe(3);
        order.Estimate.ServicesSubtotal.ShouldBe(250m);
        order.Estimate.PartsSubtotal.ShouldBe(239m);
        order.Estimate.Total.ShouldBe(489m);
    }

    [Fact]
    public void Should_Replace_Estimate_Items_And_Recalculate_Totals()
    {
        var order = CreateOrderInDiagnosisWithDiagnosis();
        order.SaveEstimate([
            new ServiceOrderEstimateItemData("Mão de obra", EstimateItemType.Service, 1, 100m),
            new ServiceOrderEstimateItemData("Peça antiga", EstimateItemType.Part, 1, 50m)
        ]);
        var replacedItemIds = order.Estimate!.Items.Select(item => item.Id).ToArray();

        order.SaveEstimate([
            new ServiceOrderEstimateItemData("Novo serviço", EstimateItemType.Service, 2, 75m)
        ]);

        order.Estimate.Items.Count.ShouldBe(1);
        order.Estimate.Items.ShouldNotContain(item => replacedItemIds.Contains(item.Id));
        order.Estimate.ServicesSubtotal.ShouldBe(150m);
        order.Estimate.PartsSubtotal.ShouldBe(0m);
        order.Estimate.Total.ShouldBe(150m);
    }

    [Fact]
    public void Should_Reject_Invalid_Estimate_Quantity()
    {
        var order = CreateOrderInDiagnosisWithDiagnosis();

        Should.Throw<ArgumentOutOfRangeException>(() => order.SaveEstimate([
            new ServiceOrderEstimateItemData("Mão de obra", EstimateItemType.Service, 0, 100m)
        ])).Message.ShouldContain("quantidade");
    }

    [Fact]
    public void Should_Reject_Invalid_Estimate_Unit_Price()
    {
        var order = CreateOrderInDiagnosisWithDiagnosis();

        Should.Throw<ArgumentOutOfRangeException>(() => order.SaveEstimate([
            new ServiceOrderEstimateItemData("Mão de obra", EstimateItemType.Service, 1, 0m)
        ])).Message.ShouldContain("valor unitário");
    }

    [Fact]
    public void Should_Not_Send_Service_Order_For_Approval_Without_Estimate()
    {
        var order = CreateOrderInDiagnosisWithDiagnosis();

        var exception = Should.Throw<InvalidOperationException>(() => SendForApproval(order));

        exception.Message.ShouldBe(
            "Registre um orçamento válido antes de enviar a ordem para aprovação.");
        order.Status.ShouldBe(ServiceOrderStatus.Diagnosis);
    }

    [Fact]
    public void Should_Send_For_Approval_With_Diagnosis_And_Valid_Estimate()
    {
        var order = CreateOrderInDiagnosisWithDiagnosis();
        order.SaveEstimate([
            new ServiceOrderEstimateItemData("Mão de obra", EstimateItemType.Service, 1, 100m)
        ]);

        SendForApproval(order);

        order.Status.ShouldBe(ServiceOrderStatus.WaitingApproval);
        order.History.Last().Description.ShouldBe(ServiceOrderMessages.SentForApproval);
    }

    private static ServiceOrderEntity CreateOrderInDiagnosisWithDiagnosis()
    {
        var order = new ServiceOrderEntity(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            "Barulho na suspensão",
            35000);
        order.StartDiagnosis();
        order.SaveDiagnosis("Folga identificada na suspensão.");
        return order;
    }

    private static ServiceOrderEstimateApprovalEntity SendForApproval(ServiceOrderEntity order)
    {
        var now = DateTimeOffset.UtcNow;
        return order.SendForApproval(Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            "protected-token", now.AddDays(7), now);
    }
}
