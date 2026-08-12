using System.ComponentModel.DataAnnotations;

namespace SemBroncaAI.Garage.Web.Models;

public sealed class GarageSettingsModel
{
    public Guid Id { get; set; }
    [Required(ErrorMessage = "Informe o nome da oficina.")] public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Informe o documento.")] public string Document { get; set; } = string.Empty;
    [Required(ErrorMessage = "Informe o telefone.")] public string Phone { get; set; } = string.Empty;
    [Required(ErrorMessage = "Informe o e-mail."), EmailAddress(ErrorMessage = "Informe um e-mail válido.")] public string Email { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Informe a UF com dois caracteres.")] public string? State { get; set; }
    public string? LogoStorageKey { get; set; }
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Use uma cor no formato #RRGGBB.")] public string? PrimaryColor { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed record UpdateGarageSettingsRequest(
    string Name, string Document, string Phone, string Email,
    string? PostalCode, string? Street, string? Number, string? Complement,
    string? Neighborhood, string? City, string? State, string? PrimaryColor);
