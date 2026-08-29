namespace SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

public sealed record EstimateApprovalSnapshotItem(Guid Id, string Description, EstimateItemType Type,
    decimal Quantity, decimal UnitPrice, decimal Total);
