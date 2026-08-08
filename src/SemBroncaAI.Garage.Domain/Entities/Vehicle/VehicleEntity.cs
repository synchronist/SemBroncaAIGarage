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
        GarageId = garageId;
        CustomerId = customerId;
        Plate = plate;
        Brand = brand;
        Model = model;
        Version = version;
        Year = year;
        Color = color;
        Fuel = fuel;
        Mileage = mileage;
        Active = true;
        CreatedAt = DateTime.UtcNow;
    }
}