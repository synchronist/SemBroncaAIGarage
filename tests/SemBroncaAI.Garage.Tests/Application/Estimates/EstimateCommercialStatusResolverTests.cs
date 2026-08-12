using SemBroncaAI.Garage.Application.Features.Estimates.ListEstimates;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using Shouldly;

namespace SemBroncaAI.Garage.Tests.Application.Estimates;

public sealed class EstimateCommercialStatusResolverTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(null, false, false, EstimateCommercialStatus.NotSent)]
    [InlineData(EstimateApprovalStatus.Pending, false, false, EstimateCommercialStatus.Pending)]
    [InlineData(EstimateApprovalStatus.Pending, true, false, EstimateCommercialStatus.Expired)]
    [InlineData(EstimateApprovalStatus.Approved, false, false, EstimateCommercialStatus.Approved)]
    [InlineData(EstimateApprovalStatus.Rejected, false, false, EstimateCommercialStatus.Rejected)]
    [InlineData(EstimateApprovalStatus.Pending, false, true, EstimateCommercialStatus.NotSent)]
    [InlineData(EstimateApprovalStatus.Rejected, false, true, EstimateCommercialStatus.NotSent)]
    public void Should_derive_current_commercial_status(EstimateApprovalStatus? status, bool expired,
        bool invalidated, EstimateCommercialStatus expected)
    {
        var result = EstimateCommercialStatusResolver.Resolve(status,
            expired ? Now : Now.AddDays(1), invalidated ? Now.AddMinutes(-1) : null, Now);
        result.ShouldBe(expected);
    }
}
