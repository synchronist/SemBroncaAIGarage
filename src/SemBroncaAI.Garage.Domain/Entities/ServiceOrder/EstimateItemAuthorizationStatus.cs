namespace SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

public enum EstimateItemAuthorizationStatus
{
    Pending = 0,
    CustomerAuthorized = 1,
    CustomerNotAuthorized = 2,
    DigitalApprovalWaived = 3
}
