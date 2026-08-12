using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.ListServiceOrders;

public static class ServiceOrderArchiveFilterExtensions
{
    public static IQueryable<ServiceOrderEntity> ApplyTenantAndArchiveFilter(
        this IQueryable<ServiceOrderEntity> source, Guid garageId, ServiceOrderArchiveFilter filter) =>
        source.Where(x => x.GarageId == garageId).ApplyArchiveFilter(filter);

    public static IQueryable<ServiceOrderEntity> ApplyArchiveFilter(
        this IQueryable<ServiceOrderEntity> source, ServiceOrderArchiveFilter filter) => filter switch
    {
        ServiceOrderArchiveFilter.Active => source.Where(x => x.ArchivedAt == null),
        ServiceOrderArchiveFilter.Archived => source.Where(x => x.ArchivedAt != null),
        _ => source
    };
}
