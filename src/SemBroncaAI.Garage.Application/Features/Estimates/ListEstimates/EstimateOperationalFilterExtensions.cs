using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Application.Features.Estimates.ListEstimates;

public static class EstimateOperationalFilterExtensions
{
    public static IQueryable<ServiceOrderEntity> ApplyOperationalEstimateFilter(
        this IQueryable<ServiceOrderEntity> source, Guid garageId) =>
        source.Where(x => x.GarageId == garageId && x.ArchivedAt == null && x.Estimate != null);
}
