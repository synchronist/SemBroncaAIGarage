using SemBroncaAI.Garage.Application.Features.Estimates.ListEstimates;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Application.Estimates;

public sealed class EstimateOperationalFilterTests
{
    [Fact]
    public void Central_should_exclude_archived_orders_and_other_garages()
    {
        var garage = Guid.NewGuid();
        var active = WithEstimate(garage, 1);
        var archived = WithEstimate(garage, 2); archived.Cancel(); archived.Archive(DateTimeOffset.UtcNow);
        var otherGarage = WithEstimate(Guid.NewGuid(), 3);

        new[] { active, archived, otherGarage }.AsQueryable()
            .ApplyOperationalEstimateFilter(garage)
            .Single().ShouldBeSameAs(active);
    }

    private static ServiceOrderEntity WithEstimate(Guid garageId, int number)
    {
        var order = new ServiceOrderEntity(garageId, Guid.NewGuid(), number, "OS", 1);
        order.StartDiagnosis(); order.SaveDiagnosis("Diagnóstico");
        order.SaveEstimate([new("Serviço", EstimateItemType.Service, 1, 10)]);
        return order;
    }
}
