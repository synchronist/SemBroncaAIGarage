using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders;

public sealed record ServiceOrderTransitionResponse(
    Guid Id,
    int Number,
    ServiceOrderStatus Status,
    int HistoryCount);