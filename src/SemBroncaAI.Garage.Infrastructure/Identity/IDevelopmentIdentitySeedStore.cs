namespace SemBroncaAI.Garage.Infrastructure.Identity;

public interface IDevelopmentIdentitySeedStore
{
    Task<bool> GarageExistsAsync(Guid garageId, CancellationToken cancellationToken);
    Task EnsureRoleAsync(string role, CancellationToken cancellationToken);
    Task<ApplicationUser?> FindUserAsync(string email, string userName, CancellationToken cancellationToken);
    Task<ApplicationUser> CreateOwnerAsync(DevelopmentIdentitySeedOptions options, CancellationToken cancellationToken);
    Task EnsureOwnerRoleAsync(ApplicationUser user, CancellationToken cancellationToken);
}
