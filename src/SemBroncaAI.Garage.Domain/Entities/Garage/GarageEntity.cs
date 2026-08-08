using SemBroncaAI.Garage.Domain.Common;
using SemBroncaAI.Garage.Domain.Entities.ServiceOrder;
using SemBroncaAI.Garage.Domain.Entities.Vehicle;

namespace SemBroncaAI.Garage.Domain.Entities.Garage;

public sealed class GarageEntity : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Document { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PostalCode { get; private set; }
    public string? Street { get; private set; }
    public string? Number { get; private set; }
    public string? Complement { get; private set; }
    public string? Neighborhood { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
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
        SetBusinessData(name, document, phone, email);
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
        SetBusinessData(Name, Document, phone, email);
    }

    public void UpdateSettings(
        string name, string document, string phone, string email,
        string? postalCode, string? street, string? number, string? complement,
        string? neighborhood, string? city, string? state)
    {
        var normalizedState = NormalizeOptional(state, nameof(state), 2)?.ToUpperInvariant();
        if (normalizedState is { Length: not 2 })
            throw new ArgumentException("A UF deve possuir dois caracteres.", nameof(state));

        SetBusinessData(name, document, phone, email);
        PostalCode = NormalizeOptional(postalCode, nameof(postalCode), 10);
        Street = NormalizeOptional(street, nameof(street), 200);
        Number = NormalizeOptional(number, nameof(number), 20);
        Complement = NormalizeOptional(complement, nameof(complement), 100);
        Neighborhood = NormalizeOptional(neighborhood, nameof(neighborhood), 100);
        City = NormalizeOptional(city, nameof(city), 100);
        State = normalizedState;
    }

    private void SetBusinessData(string name, string document, string phone, string email)
    {
        var normalizedName = Require(name, nameof(name), "O nome da oficina é obrigatório.", 150);
        var normalizedDocument = Require(document, nameof(document), "O documento da oficina é obrigatório.", 20);
        var normalizedPhone = Require(phone, nameof(phone), "O telefone da oficina é obrigatório.", 20);
        var normalizedEmail = Require(email, nameof(email), "O e-mail da oficina é obrigatório.", 150);
        if (!normalizedEmail.Contains('@') || normalizedEmail.StartsWith('@') || normalizedEmail.EndsWith('@'))
            throw new ArgumentException("O e-mail informado é inválido.", nameof(email));
        Name = normalizedName;
        Document = normalizedDocument;
        Phone = normalizedPhone;
        Email = normalizedEmail;
    }

    private static string Require(string value, string parameter, string message, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(message, parameter);
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
            throw new ArgumentException($"O campo {parameter} deve possuir no máximo {maximumLength} caracteres.", parameter);
        return normalized;
    }

    private static string? NormalizeOptional(string? value, string parameter, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
            throw new ArgumentException($"O campo {parameter} deve possuir no máximo {maximumLength} caracteres.", parameter);
        return normalized;
    }
}
