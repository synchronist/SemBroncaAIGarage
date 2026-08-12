using Microsoft.AspNetCore.Identity;
using SemBroncaAI.Garage.Domain.Entities.Garage;

namespace SemBroncaAI.Garage.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;
    public Guid? GarageId { get; set; }
    public GarageEntity? Garage { get; private set; }
    public bool Active { get; private set; } = true;

    public void Deactivate() => Active = false;
    public void Activate() => Active = true;

    public static ApplicationUser CreateGarageUser(string name, string email, string? userName, Guid garageId)
    {
        if (garageId == Guid.Empty) throw new ArgumentException("A oficina é obrigatória.", nameof(garageId));
        return Create(name, email, userName, garageId);
    }

    public static ApplicationUser CreatePlatformAdmin(string name, string email, string? userName) =>
        Create(name, email, userName, null);

    private static ApplicationUser Create(string name, string email, string? userName, Guid? garageId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("O nome é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("O e-mail é obrigatório.", nameof(email));
        return new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            Email = email.Trim(),
            UserName = string.IsNullOrWhiteSpace(userName) ? email.Trim() : userName.Trim(),
            GarageId = garageId,
            Active = true
        };
    }
}
