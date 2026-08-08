using SemBroncaAI.Garage.Domain.Common;
using SemBroncaAI.Garage.Domain.Entities.Garage;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;


namespace SemBroncaAI.Garage.Domain.Entities.Customer;

public sealed class CustomerEntity : Entity
{
    public Guid GarageId { get; private set; }

    public GarageEntity Garage { get; private set; } = default!;

    public ICollection<VehicleEntity> Vehicles { get; private set; } = [];

    public string Name { get; private set; } = string.Empty;

    public string Document { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public bool Active { get; private set; }

    private CustomerEntity()
    {
    }

    public CustomerEntity(
        Guid garageId,
        string name,
        string document,
        string phone,
        string email)
    {
        GarageId = garageId;
        Name = name;
        Document = document;
        Phone = phone;
        Email = email;
        Active = true;
        CreatedAt = DateTime.UtcNow;
    }
}