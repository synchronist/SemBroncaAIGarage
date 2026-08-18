using SemBroncaAI.Garage.Web.Models;

namespace SemBroncaAI.Garage.Web.Services;

public static class ServiceOrderPrintBuilder
{
    public const string DefaultPrimaryColor = "#F97316";
    public static ServiceOrderPrintModel Build(ServiceOrderDetailsModel order, GarageSettingsModel garage, string? logoUrl = null) =>
        new(order.Id, order.Number, order.Status, order.CustomerComplaint, order.Mileage, order.CreatedAt,
            new(garage.Name, garage.Document, garage.Phone, garage.Email, garage.PostalCode, garage.Street,
                garage.Number, garage.Complement, garage.Neighborhood, garage.City, garage.State,
                garage.LogoStorageKey is null ? null : logoUrl, garage.PrimaryColor ?? DefaultPrimaryColor),
            new(order.Customer.Name, order.Customer.Document ?? string.Empty,
                order.Customer.Phone ?? string.Empty, order.Customer.Email ?? string.Empty),
            new(order.Vehicle.Plate, order.Vehicle.Brand, order.Vehicle.Model, order.Vehicle.Version,
                order.Vehicle.Year, order.Vehicle.Color, order.Vehicle.Fuel),
            order.Diagnosis is null ? null : new DiagnosisPrintModel(order.Diagnosis.Description),
            order.Estimate is null ? null : new EstimatePrintModel(order.Estimate.ServicesSubtotal,
                order.Estimate.PartsSubtotal, order.Estimate.Total, order.Estimate.Items.Select(item =>
                    new EstimateItemPrintModel(item.Type, item.Description, item.Quantity, item.UnitPrice, item.Total)).ToArray()));
}
