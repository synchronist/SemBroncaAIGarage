namespace SemBroncaAI.Garage.Web.Models;

public sealed record ServiceOrderPrintModel(
    Guid Id, int Number, string Status, string CustomerComplaint, int? Mileage, DateTimeOffset CreatedAt,
    GaragePrintModel Garage, CustomerPrintModel Customer, VehiclePrintModel Vehicle,
    DiagnosisPrintModel? Diagnosis, EstimatePrintModel? Estimate);

public sealed record GaragePrintModel(string Name, string Document, string Phone, string Email,
    string? PostalCode, string? Street, string? Number, string? Complement, string? Neighborhood, string? City, string? State,
    string? LogoUrl, string PrimaryColor);
public sealed record CustomerPrintModel(string Name, string Document, string Phone, string Email);
public sealed record VehiclePrintModel(string Plate, string Brand, string Model, string Version, int Year, string Color, string Fuel);
public sealed record DiagnosisPrintModel(string Description);
public sealed record EstimatePrintModel(decimal ServicesSubtotal, decimal PartsSubtotal, decimal Total,
    IReadOnlyCollection<EstimateItemPrintModel> Items, string? ApprovalStatus = null,
    decimal? ApprovedTotal = null, bool DigitalApprovalWaived = false);
public sealed record EstimateItemPrintModel(string Type, string Description, decimal Quantity, decimal UnitPrice,
    decimal Total, Guid? Id = null, string AuthorizationStatus = "Pending");
