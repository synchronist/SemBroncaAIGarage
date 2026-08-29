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
        Approve(order, approval, now.AddMinutes(1));
        order.Estimate!.Items.ShouldAllBe(item =>
            item.AuthorizationStatus == EstimateItemAuthorizationStatus.CustomerAuthorized);
        order.StartService();
        order.Status.ShouldBe(ServiceOrderStatus.InProgress);
    }

    [Fact]
    public void Rejected_Estimate_Should_Block_Service_And_Record_Comment()
    {
        var (order, approval, now) = PendingOrder();
        order.RejectEstimate(approval.Id, "Cliente da Silva", "52998224725", "11999999999",
            "Revisar o valor das peças.", null, null, now.AddMinutes(1));
        approval.Status.ShouldBe(EstimateApprovalStatus.Rejected);
        approval.IsActive.ShouldBeTrue();
        approval.CustomerComment.ShouldBe("Revisar o valor das peças.");
        approval.RespondedAt.ShouldNotBeNull();
        order.Estimate!.Items.ShouldAllBe(item =>
            item.AuthorizationStatus == EstimateItemAuthorizationStatus.CustomerNotAuthorized);
        Should.Throw<InvalidOperationException>(() => order.StartService());
    }

    [Fact]
    public void Approval_Response_Should_Not_Be_Changed()
    {
        var (order, approval, now) = PendingOrder();
        Approve(order, approval, now.AddMinutes(1));
        Should.Throw<InvalidOperationException>(() => order.RejectEstimate(approval.Id, "Cliente da Silva",
            "52998224725", "11999999999", null, null, null, now.AddMinutes(2)));
    }

    [Fact]
    public void Expired_Approval_Should_Not_Be_Accepted()
    {
        var (order, approval, now) = PendingOrder(TimeSpan.FromMinutes(1));
        Should.Throw<InvalidOperationException>(() => Approve(order, approval, now.AddMinutes(2)))
            .Message.ShouldContain("expirou");
    }

    [Fact]
    public void Revising_Rejected_Estimate_Should_Invalidate_Old_Token()
    {
        var (order, approval, now) = PendingOrder();
        order.RejectEstimate(approval.Id, "Cliente da Silva", "52998224725", "11999999999",
            null, null, null, now.AddMinutes(1));
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
        order.Estimate!.Items.ShouldAllBe(item =>
            item.AuthorizationStatus == EstimateItemAuthorizationStatus.DigitalApprovalWaived);
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

    [Fact]
    public void Partial_Approval_Should_Record_Only_Selected_Value_And_Allow_Service()
    {
        var (order, approval, now) = PendingOrder();
        var selected = order.Estimate!.Items.First();

        order.ApproveEstimate(approval.Id, "Cliente da Silva", "52998224725", "11999999999",
            [selected.Id], "Aprovo somente o serviço.", "127.0.0.1", "browser", now.AddMinutes(1));

        approval.Status.ShouldBe(EstimateApprovalStatus.PartiallyApproved);
        approval.ApprovedTotal.ShouldBe(selected.Total);
        approval.CustomerDocument.ShouldBe("52998224725");
        approval.ClientIp.ShouldBe("127.0.0.1");
        selected.AuthorizationStatus.ShouldBe(EstimateItemAuthorizationStatus.CustomerAuthorized);
        order.Estimate.Items.Single(item => item.Id != selected.Id).AuthorizationStatus
            .ShouldBe(EstimateItemAuthorizationStatus.CustomerNotAuthorized);
        order.StartService();
        order.Status.ShouldBe(ServiceOrderStatus.InProgress);
        order.History.Last().Description.ShouldBe(ServiceOrderMessages.PartiallyApprovedServiceStarted);
    }

    [Fact]
    public void Customer_Decision_Should_Not_Be_Replaced_By_Digital_Approval_Waiver()
    {
        var (order, approval, now) = PendingOrder();
        Approve(order, approval, now.AddMinutes(1));

        Should.Throw<InvalidOperationException>(() =>
            order.WaiveDigitalApproval(now.AddMinutes(2), Guid.NewGuid()))
            .Message.ShouldContain("decisão do cliente");
        approval.Status.ShouldBe(EstimateApprovalStatus.Approved);
        order.DigitalApprovalWaivedAt.ShouldBeNull();
    }

    [Fact]
    public void Approval_Should_Preserve_The_Presented_Estimate_Snapshot()
    {
        var order = DiagnosedOrder();
        var presentedItems = order.Estimate!.Items.Select(item => new { item.Id, item.Description, item.Total }).ToArray();
        var now = DateTimeOffset.UtcNow;
        var approval = order.SendForApproval("A".PadLeft(64, 'A'), "protected", now.AddDays(7), now);

        var snapshot = System.Text.Json.JsonSerializer.Deserialize<EstimateApprovalSnapshotItem[]>(approval.EstimateSnapshotJson)!;
        snapshot[0].Id.ShouldBe(presentedItems[0].Id);
        snapshot[0].Description.ShouldBe(presentedItems[0].Description);
        approval.EstimateTotal.ShouldBe(presentedItems.Sum(item => item.Total));
    }

    private static void Approve(ServiceOrderEntity order, ServiceOrderEstimateApprovalEntity approval,
        DateTimeOffset now) => order.ApproveEstimate(approval.Id, "Cliente da Silva", "52998224725",
            "11999999999", order.Estimate!.Items.Select(item => item.Id).ToArray(), null, null, null, now);

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
