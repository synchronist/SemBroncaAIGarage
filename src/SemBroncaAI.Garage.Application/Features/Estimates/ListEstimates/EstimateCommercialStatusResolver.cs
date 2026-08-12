using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Application.Features.Estimates.ListEstimates;

public static class EstimateCommercialStatusResolver
{
    public static EstimateCommercialStatus Resolve(EstimateApprovalStatus? status,
        DateTimeOffset? expiresAt, DateTimeOffset? invalidatedAt, DateTimeOffset now)
    {
        if (status is null || invalidatedAt is not null) return EstimateCommercialStatus.NotSent;
        return status switch
        {
            EstimateApprovalStatus.Approved => EstimateCommercialStatus.Approved,
            EstimateApprovalStatus.Rejected => EstimateCommercialStatus.Rejected,
            EstimateApprovalStatus.Pending when expiresAt <= now => EstimateCommercialStatus.Expired,
            EstimateApprovalStatus.Pending => EstimateCommercialStatus.Pending,
            _ => EstimateCommercialStatus.NotSent
        };
    }
}
