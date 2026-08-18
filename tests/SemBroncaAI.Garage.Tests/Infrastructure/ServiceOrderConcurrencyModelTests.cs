using Microsoft.EntityFrameworkCore;
using SemBroncaAI.Garage.Infrastructure.Persistence;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Infrastructure;

public sealed class ServiceOrderConcurrencyModelTests
{
    [Fact]
    public void Model_should_enforce_unique_service_order_number_per_garage()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(
            "SemBroncaAI.Garage.Domain.Entities.ServiceOrder.ServiceOrderEntity")!;

        var index = entity.GetIndexes().Single(candidate =>
            candidate.Properties.Select(property => property.Name)
                .SequenceEqual(["GarageId", "Number"]));

        index.IsUnique.ShouldBeTrue();
        index.GetDatabaseName().ShouldBe(DatabaseConstraintNames.UniqueServiceOrderNumberPerGarage);
    }

    [Fact]
    public void Model_should_allow_only_one_active_pending_approval_per_estimate_version()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(
            "SemBroncaAI.Garage.Domain.Entities.ServiceOrder.ServiceOrderEstimateApprovalEntity")!;

        var index = entity.GetIndexes().Single(candidate =>
            candidate.GetDatabaseName() == ApprovalRequestPersistence.ActiveApprovalConstraint);

        index.IsUnique.ShouldBeTrue();
        index.GetFilter().ShouldBe("\"Status\" = 1 AND \"InvalidatedAt\" IS NULL");
    }

    [Fact]
    public void Number_sequence_should_be_scoped_by_garage()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ServiceOrderNumberSequence))!;

        entity.FindPrimaryKey()!.Properties.Single().Name.ShouldBe("GarageId");
    }

    [Fact]
    public void Service_order_version_should_be_an_explicit_concurrency_token()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(
            "SemBroncaAI.Garage.Domain.Entities.ServiceOrder.ServiceOrderEntity")!;

        entity.FindProperty("Version")!.IsConcurrencyToken.ShouldBeTrue();
    }

    [Fact]
    public void New_service_order_should_start_at_version_one()
    {
        var order = new ServiceOrderEntity(Guid.NewGuid(), Guid.NewGuid(), 1, "Relato", 0);
        order.Version.ShouldBe(1);
    }

    [Fact]
    public void Changed_approval_should_increment_the_aggregate_root_version()
    {
        using var context = CreateContext();
        var order = new ServiceOrderEntity(Guid.NewGuid(), Guid.NewGuid(), 1, "Relato", 0);
        order.StartDiagnosis();
        order.SaveDiagnosis("Diagnóstico");
        order.SaveEstimate([new("Serviço", EstimateItemType.Service, 1, 100m)]);
        var now = DateTimeOffset.UtcNow;
        var approval = order.SendForApproval("A".PadLeft(64, 'A'), "protected", now.AddDays(7), now);
        context.Attach(order);

        order.ApproveEstimate(approval.Id, "Cliente", now.AddMinutes(1));
        typeof(GarageDbContext).GetMethod(
            "IncrementServiceOrderVersions",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(context, null);

        order.Version.ShouldBe(2);
        context.Entry(order).State.ShouldBe(EntityState.Modified);
    }

    private static GarageDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GarageDbContext>()
            .UseNpgsql("Host=localhost;Database=model-only;Username=model-only;Password=model-only")
            .Options;
        return new GarageDbContext(options);
    }
}
