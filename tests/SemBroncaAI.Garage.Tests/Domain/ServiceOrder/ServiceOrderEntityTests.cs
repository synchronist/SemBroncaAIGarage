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
            "Barulho na suspensão");

        // Assert
        order.GarageId.ShouldBe(garageId);
        order.VehicleId.ShouldBe(vehicleId);
        order.Number.ShouldBe(1);
        order.Status.ShouldBe(ServiceOrderStatus.Received);
        order.CustomerComplaint.ShouldBe("Barulho na suspensão");

        order.History.Count.ShouldBe(1);
    }

    [Fact]
    public void Should_Start_Diagnosis_And_Add_History()
    {
        var order = new ServiceOrderEntity(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            "Barulho na suspensão");

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
            "Barulho na suspensão");

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
            "Barulho na suspensão");

        order.StartDiagnosis();

        order.SaveDiagnosis(
            "Folga identificada na bieleta dianteira direita.",
            "Verificar também as buchas da bandeja.");

        order.SendForApproval();

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
            "Barulho na suspensão");

        order.StartDiagnosis();

        var exception = Should.Throw<InvalidOperationException>(
            () => order.SendForApproval());

        exception.Message.ShouldBe(
            "Registre o diagnóstico antes de enviar a ordem para aprovação.");

        order.Status.ShouldBe(
            ServiceOrderStatus.Diagnosis);
    }
}