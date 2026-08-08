using SemBroncaAI.Garage.Domain.Common;
using SemBroncaAI.Garage.Domain.Entities.Customer;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;

namespace SemBroncaAI.Garage.Domain.Entities.Vehicle;

public sealed class VehicleEntity : Entity
{
    public Guid GarageId { get; private set; }

    public GarageEntity Garage { get; private set; } = default!;

    public Guid CustomerId { get; private set; }

    public CustomerEntity Customer { get; private set; } = default!;

    public ICollection<ServiceOrderEntity> ServiceOrders { get; private set; } = [];

    public string Plate { get; private set; } = string.Empty;

    public string Brand { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    public string Version { get; private set; } = string.Empty;

    public int Year { get; private set; }

    public string Color { get; private set; } = string.Empty;

    public string Fuel { get; private set; } = string.Empty;

    public int Mileage { get; private set; }

    public bool Active { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private VehicleEntity()
    {
    }

    public VehicleEntity(
        Guid garageId,
        Guid customerId,
        string plate,
        string brand,
        string model,
        string version,
        int year,
        string color,
        string fuel,
        int mileage)
    {
        GarageId = Guard.AgainstEmpty(garageId, nameof(garageId));
        CustomerId = Guard.AgainstEmpty(customerId, nameof(customerId));
        SetDetails(plate, brand, model, version, year, color, fuel, mileage);
        Active = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(Guid customerId, string plate, string brand, string model, string version, int year, string color, string fuel, int mileage)
    {
        CustomerId = Guard.AgainstEmpty(customerId, nameof(customerId));
        SetDetails(plate, brand, model, version, year, color, fuel, mileage);
    }

    public void UpdateMileage(int mileage)
    {
        if (mileage < 0) throw new ArgumentOutOfRangeException(nameof(mileage), "A quilometragem não pode ser negativa.");
        Mileage = mileage;
    }

    public static string NormalizePlate(string plate) =>
        Guard.AgainstNullOrWhiteSpace(plate, nameof(plate)).Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();

    private void SetDetails(string plate, string brand, string model, string version, int year, string color, string fuel, int mileage)
    {
        Plate = NormalizePlate(plate);
        Brand = Guard.AgainstNullOrWhiteSpace(brand, nameof(brand));
        Model = Guard.AgainstNullOrWhiteSpace(model, nameof(model));
        Version = version?.Trim() ?? string.Empty;
        Color = color?.Trim() ?? string.Empty;
        Fuel = fuel?.Trim() ?? string.Empty;
        if (year < 1900 || year > DateTime.UtcNow.Year + 1) throw new ArgumentOutOfRangeException(nameof(year), "O ano do veículo é inválido.");
        if (mileage < 0) throw new ArgumentOutOfRangeException(nameof(mileage), "A quilometragem não pode ser negativa.");
        Year = year;
        Mileage = mileage;
    }
}
