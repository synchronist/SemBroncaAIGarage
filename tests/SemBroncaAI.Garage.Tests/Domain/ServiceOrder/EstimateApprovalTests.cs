using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Domain.ServiceOrder;

public sealed class EstimateApprovalTests
{
    [Fact]
    public void Start_Service_Should_Require_Approved_Estimate()
    {
        var (order, approval, now) = PendingOrder();
        Should.Throw<InvalidOperationException>(() => order.StartService()).Message.ShouldContain("aprovação");
        order.ApproveEstimate(approval.Id, "Cliente", now.AddMinutes(1));
        order.StartService();
        order.Status.ShouldBe(ServiceOrderStatus.InProgress);
    }

    [Fact]
    public void Rejected_Estimate_Should_Block_Service_And_Record_Comment()
    {
        var (order, approval, now) = PendingOrder();
        order.RejectEstimate(approval.Id, "Cliente", "Revisar o valor das peças.", now.AddMinutes(1));
        approval.Status.ShouldBe(EstimateApprovalStatus.Rejected);
        approval.IsActive.ShouldBeTrue();
        approval.CustomerComment.ShouldBe("Revisar o valor das peças.");
        approval.RespondedAt.ShouldNotBeNull();
        Should.Throw<InvalidOperationException>(() => order.StartService());
    }

    [Fact]
    public void Approval_Response_Should_Not_Be_Changed()
    {
        var (order, approval, now) = PendingOrder();
        order.ApproveEstimate(approval.Id, null, now.AddMinutes(1));
        Should.Throw<InvalidOperationException>(() => order.RejectEstimate(approval.Id, null, null, now.AddMinutes(2)));
    }

    [Fact]
    public void Expired_Approval_Should_Not_Be_Accepted()
    {
        var (order, approval, now) = PendingOrder(TimeSpan.FromMinutes(1));
        Should.Throw<InvalidOperationException>(() => order.ApproveEstimate(approval.Id, null, now.AddMinutes(2)))
            .Message.ShouldContain("expirou");
    }

    [Fact]
    public void Revising_Rejected_Estimate_Should_Invalidate_Old_Token()
    {
        var (order, approval, now) = PendingOrder();
        order.RejectEstimate(approval.Id, null, null, now.AddMinutes(1));
        order.ReviseRejectedEstimate();
        order.SaveEstimate([new("Novo serviço", EstimateItemType.Service, 1, 200m)]);
        var replacement = order.SendForApproval("B".PadLeft(64, 'B'), "protected-2", now.AddDays(7), now.AddMinutes(2));
        approval.Status.ShouldBe(EstimateApprovalStatus.Rejected);
        approval.IsActive.ShouldBeFalse();
        replacement.Status.ShouldBe(EstimateApprovalStatus.Pending);
        order.CurrentEstimateApproval.ShouldBe(replacement);
    }

    [Fact]
    public void Waiving_Digital_Approval_Should_Record_Actor_Without_Creating_Fake_Approval()
    {
        var actorId = Guid.NewGuid();
        var order = DiagnosedOrder();
        var now = DateTimeOffset.UtcNow;

        order.WaiveDigitalApproval(now, actorId);

        order.Status.ShouldBe(ServiceOrderStatus.WaitingApproval);
        order.DigitalApprovalWaivedAt.ShouldBe(now);
        order.EstimateApprovals.ShouldBeEmpty();
        var history = order.History.Last();
        history.Description.ShouldBe(ServiceOrderMessages.DigitalApprovalWaived);
        history.ActorId.ShouldBe(actorId);
        history.CreatedAt.ShouldBeInRange(now.AddSeconds(-1), now.AddSeconds(1));
    }

    [Fact]
    public void Waived_Digital_Approval_Should_Allow_Service_To_Start()
    {
        var order = DiagnosedOrder();
        order.WaiveDigitalApproval(DateTimeOffset.UtcNow, Guid.NewGuid());

        order.StartService(Guid.NewGuid());

        order.Status.ShouldBe(ServiceOrderStatus.InProgress);
        order.CurrentEstimateApproval.ShouldBeNull();
    }

    [Fact]
    public void Pending_Digital_Approval_Should_Be_Waivable_Without_Creating_A_Fake_Approval()
    {
        var (order, approval, now) = PendingOrder();
        var actorId = Guid.NewGuid();

        order.WaiveDigitalApproval(now.AddMinutes(1), actorId);

        order.Status.ShouldBe(ServiceOrderStatus.WaitingApproval);
        order.DigitalApprovalWaivedAt.ShouldBe(now.AddMinutes(1));
        approval.Status.ShouldBe(EstimateApprovalStatus.Pending);
        approval.IsActive.ShouldBeFalse();
        order.History.Last().ActorId.ShouldBe(actorId);

        order.StartService(actorId);
        order.Status.ShouldBe(ServiceOrderStatus.InProgress);
    }

    [Fact]
    public void Waiving_Digital_Approval_Should_Require_Diagnosis_And_Valid_Estimate()
    {
        var order = new ServiceOrderEntity(Guid.NewGuid(), Guid.NewGuid(), 1, "Ruído", 1000);
        order.StartDiagnosis();

        Should.Throw<InvalidOperationException>(() =>
            order.WaiveDigitalApproval(DateTimeOffset.UtcNow));
    }

    private static (ServiceOrderEntity Order, ServiceOrderEstimateApprovalEntity Approval, DateTimeOffset Now) PendingOrder(TimeSpan? lifetime = null)
    {
        var order = DiagnosedOrder();
        var now = DateTimeOffset.UtcNow;
        var approval = order.SendForApproval("A".PadLeft(64, 'A'), "protected", now.Add(lifetime ?? TimeSpan.FromDays(7)), now);
        return (order, approval, now);
    }

    private static ServiceOrderEntity DiagnosedOrder()
    {
        var order = new ServiceOrderEntity(Guid.NewGuid(), Guid.NewGuid(), 1, "Ruído", 1000);
        order.StartDiagnosis();
        order.SaveDiagnosis("Diagnóstico público", "Nota interna secreta");
        order.SaveEstimate([new("Serviço", EstimateItemType.Service, 1, 100m), new("Peça", EstimateItemType.Part, 2, 25m)]);
        return order;
    }
}
