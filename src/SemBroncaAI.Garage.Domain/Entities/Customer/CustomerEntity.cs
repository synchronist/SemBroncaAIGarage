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
        GarageId = Guard.AgainstEmpty(garageId, nameof(garageId));
        SetContactInformation(name, document, phone, email);
        Active = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string name,
        string document,
        string phone,
        string email)
    {
        SetContactInformation(name, document, phone, email);
    }

    private void SetContactInformation(
        string name,
        string document,
        string phone,
        string email)
    {
        Name = Guard.RequiredWithMaximumLength(name, FieldLengthLimits.PersonName, nameof(name));
        if (!BrazilianDocument.IsValid(document))
            throw new ArgumentException("Informe um CPF ou CNPJ válido.", nameof(document));
        Document = BrazilianDocument.Normalize(document);
        if (!BrazilianPhone.IsValid(phone))
            throw new ArgumentException("Informe um telefone válido.", nameof(phone));
        Phone = BrazilianPhone.Normalize(phone);
        Email = Guard.RequiredWithMaximumLength(email, FieldLengthLimits.Email, nameof(email));

        if (!Email.Contains('@') || Email.StartsWith('@') || Email.EndsWith('@'))
        {
            throw new ArgumentException("O e-mail informado é inválido.", nameof(email));
        }
    }
}
