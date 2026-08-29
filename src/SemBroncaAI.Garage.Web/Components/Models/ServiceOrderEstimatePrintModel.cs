namespace SemBroncaAI.Garage.Web.Models;

public sealed record ServiceOrderEstimatePrintModel(
    Guid ServiceOrderId, int ServiceOrderNumber, DateTimeOffset CreatedAt, string CustomerComplaint, int? Mileage,
    GaragePrintModel Garage, EstimateCustomerPrintModel Customer, EstimateVehiclePrintModel Vehicle,
    EstimateDiagnosisPrintModel? Diagnosis, EstimatePrintModel Estimate,
    ServiceOrderApprovalModel? Approval = null);

public sealed record EstimateCustomerPrintModel(string Name, string Document, string Phone, string Email);
public sealed record EstimateVehiclePrintModel(string Plate, string Brand, string Model, string Version, int Year);
public sealed record EstimateDiagnosisPrintModel(string Description);
