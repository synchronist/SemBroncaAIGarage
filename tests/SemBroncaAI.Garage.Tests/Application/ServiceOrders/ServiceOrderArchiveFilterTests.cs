using SemBroncaAI.Garage.Application.Features.ServiceOrders.ListServiceOrders;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Application.ServiceOrders;

public sealed class ServiceOrderArchiveFilterTests
{
    [Fact]
    public void Default_should_exclude_archived_and_filters_should_combine_with_pagination()
    {
        var orders = Enumerable.Range(1, 5).Select(Create).ToArray();
        orders[1].Cancel(); orders[1].Archive(DateTimeOffset.UtcNow);
        orders[3].Cancel(); orders[3].Archive(DateTimeOffset.UtcNow);

        orders.AsQueryable().ApplyArchiveFilter(ServiceOrderArchiveFilter.Active).Count().ShouldBe(3);
        orders.AsQueryable().ApplyArchiveFilter(ServiceOrderArchiveFilter.Archived).Select(x => x.Number).ShouldBe([2, 4]);
        orders.AsQueryable().ApplyArchiveFilter(ServiceOrderArchiveFilter.All).Count().ShouldBe(5);
        orders.AsQueryable().ApplyArchiveFilter(ServiceOrderArchiveFilter.Active).OrderBy(x => x.Number).Skip(1).Take(1).Single().Number.ShouldBe(3);
    }

    [Fact]
    public void Should_isolate_archive_listing_by_garage()
    {
        var garage = Guid.NewGuid();
        var own = new ServiceOrderEntity(garage, Guid.NewGuid(), 1, "OS", 1);
        var other = new ServiceOrderEntity(Guid.NewGuid(), Guid.NewGuid(), 2, "OS", 2);

        new[] { own, other }.AsQueryable()
            .ApplyTenantAndArchiveFilter(garage, ServiceOrderArchiveFilter.All)
            .Single().ShouldBeSameAs(own);
    }

    private static ServiceOrderEntity Create(int number) => new(Guid.NewGuid(), Guid.NewGuid(), number, "OS", number);
}
