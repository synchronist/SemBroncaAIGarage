using SemBroncaAI.Garage.Application.Abstractions.Persistence;
using SemBroncaAI.Garage.Application.Features.Estimates.ListEstimates;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Application.Estimates;

public sealed class ListEstimatesHandlerTests
{
    [Fact]
    public async Task Should_require_garage_id()
    {
        var handler = new ListEstimatesHandler(new RepositoryStub());
        await Should.ThrowAsync<ArgumentException>(() => handler.HandleAsync(new(Guid.Empty, null, null)));
    }

    [Fact]
    public async Task Should_forward_tenant_search_filter_and_pagination_to_read_repository()
    {
        var repository = new RepositoryStub();
        var handler = new ListEstimatesHandler(repository);
        var garageId = Guid.NewGuid();

        var response = await handler.HandleAsync(new(garageId, "Gol", EstimateCommercialStatus.Pending, 2, 5));

        repository.Query.ShouldNotBeNull();
        repository.Query.GarageId.ShouldBe(garageId);
        repository.Query.Search.ShouldBe("Gol");
        repository.Query.Status.ShouldBe(EstimateCommercialStatus.Pending);
        repository.Query.Page.ShouldBe(2);
        repository.Query.PageSize.ShouldBe(5);
        response.Indicators.Pending.ShouldBe(3);
        response.Indicators.PendingValue.ShouldBe(1250m);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Should_reject_invalid_pagination(int page, int pageSize)
    {
        var handler = new ListEstimatesHandler(new RepositoryStub());
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            handler.HandleAsync(new(Guid.NewGuid(), null, null, page, pageSize)));
    }

    private sealed class RepositoryStub : IEstimateQueryRepository
    {
        public ListEstimatesQuery? Query { get; private set; }
        public Task<ListEstimatesResponse> ListAsync(ListEstimatesQuery query, CancellationToken cancellationToken = default)
        {
            Query = query;
            return Task.FromResult(new ListEstimatesResponse(query.Page, query.PageSize, 8, 2, [], new(3, 2, 1, 1250m)));
        }
    }
}
