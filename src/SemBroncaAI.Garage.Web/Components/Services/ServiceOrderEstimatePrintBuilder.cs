using SemBroncaAI.Garage.Web.Models;

namespace SemBroncaAI.Garage.Web.Services;

public static class ServiceOrderEstimatePrintBuilder
{
    public static ServiceOrderEstimatePrintModel? Build(ServiceOrderDetailsModel order, GarageSettingsModel garage, string? logoUrl = null)
    {
        if (order.Estimate is null) return null;
        return new(order.Id, order.Number, order.CreatedAt, order.CustomerComplaint, order.Mileage,
            new(garage.Name, garage.Document, garage.Phone, garage.Email, garage.PostalCode, garage.Street,
                garage.Number, garage.Complement, garage.Neighborhood, garage.City, garage.State,
                garage.LogoStorageKey is null ? null : logoUrl, garage.PrimaryColor ?? ServiceOrderPrintBuilder.DefaultPrimaryColor),
            new(order.Customer.Name, order.Customer.Document ?? string.Empty,
                order.Customer.Phone ?? string.Empty, order.Customer.Email ?? string.Empty),
            new(order.Vehicle.Plate, order.Vehicle.Brand, order.Vehicle.Model, order.Vehicle.Version, order.Vehicle.Year),
            order.Diagnosis is null ? null : new EstimateDiagnosisPrintModel(order.Diagnosis.Description),
            new(order.Estimate.ServicesSubtotal, order.Estimate.PartsSubtotal, order.Estimate.Total,
                order.Estimate.Items.Select(item => new EstimateItemPrintModel(
                    item.Type, item.Description, item.Quantity, item.UnitPrice, item.Total)).ToArray()));
    }
}
