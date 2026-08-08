using SemBroncaAI.Garage.Domain.Common;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;

namespace SemBroncaAI.Garage.Domain.Entities.Garage;

public sealed class GarageEntity : Entity
{
    public string Name { get; private set; }
    public string Document { get; private set; }
    public string Phone { get; private set; }
    public string Email { get; private set; }
    public bool Active { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public ICollection<VehicleEntity> Vehicles { get; private set; } = [];
    public ICollection<ServiceOrderEntity> ServiceOrders { get; private set; } = [];

    public GarageEntity(
        string name,
        string document,
        string phone,
        string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "O nome da oficina é obrigatório.",
                nameof(name));

        Name = name;
        Document = document;
        Phone = phone;
        Email = email;
        Active = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Active = false;
    }

    public void Activate()
    {
        Active = true;
    }

    public void ChangeContactInformation(
        string phone,
        string email)
    {
        Phone = phone;
        Email = email;
    }
}