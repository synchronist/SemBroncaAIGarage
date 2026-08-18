namespace SemBroncaAI.Garage.Infrastructure.Identity;

public interface IDevelopmentIdentitySeedStore
{
    Task<bool> GarageExistsAsync(Guid garageId, CancellationToken cancellationToken);
    Task EnsureRoleAsync(string role, CancellationToken cancellationToken);
    Task<ApplicationUser?> FindUserAsync(string email, string userName, CancellationToken cancellationToken);
    Task<ApplicationUser> CreateUserAsync(DevelopmentSeedUser user, Guid? garageId, CancellationToken cancellationToken);
    Task EnsureRoleAsync(ApplicationUser user, string role, CancellationToken cancellationToken);
}

public sealed record DevelopmentSeedUser(string Name, string Email, string UserName, string Password, string Role);
