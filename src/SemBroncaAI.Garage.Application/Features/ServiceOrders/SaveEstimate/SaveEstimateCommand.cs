using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Application.Features.ServiceOrders.SaveEstimate;

public sealed record SaveEstimateCommand(
    IReadOnlyCollection<SaveEstimateItemCommand> Items);

public sealed record SaveEstimateItemCommand(
    string Description,
    EstimateItemType Type,
    decimal Quantity,
    decimal UnitPrice);
